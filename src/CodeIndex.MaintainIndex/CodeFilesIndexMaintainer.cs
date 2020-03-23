using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CodeIndex.Common;
using CodeIndex.Files;
using CodeIndex.IndexBuilder;
using CodeIndex.Search;
using Lucene.Net.Index;
using Lucene.Net.Search;

namespace CodeIndex.MaintainIndex
{
    public class CodeFilesIndexMaintainer : IDisposable
    {
        public CodeFilesIndexMaintainer(CodeIndexConfiguration config, ILog log)
        {
            config.RequireNotNull(nameof(config));
            config.ExcludedPathsArray.RequireNotNull(nameof(config.ExcludedPathsArray));
            config.SaveIntervalSeconds.RequireRange(nameof(config.SaveIntervalSeconds), 3600, 1);

            this.config = config;
            excludedExtensions = config.ExcludedExtensionsArray.Select(u => u.ToUpperInvariant()).ToArray();
            excludedPaths = FilePathHelper.GetPaths(config.ExcludedPathsArray, config.IsInLinux);
            saveIntervalSeconds = config.SaveIntervalSeconds;
            includedExtensions = config.IncludedExtensionsArray?.Select(u => u.ToUpperInvariant()).ToArray() ?? Array.Empty<string>();
            this.log = log;
            tokenSource = new CancellationTokenSource();
        }

        public void SetInitalizeFinishedToTrue(List<FileInfo> initalizeFailedFiles = null)
        {
            if (initalizeFailedFiles?.Count > 0)
            {
                var retryDate = DateTime.UtcNow.AddDays(-1);

                foreach (var failedFiles in initalizeFailedFiles)
                {
                    pendingRetryCodeSources.Enqueue(new PendingRetrySource
                    {
                        ChangesType = WatcherChangeTypes.Created,
                        FilePath = failedFiles.FullName,
                        LastRetryUTCDate = retryDate
                    });
                }
            }

            InitializeFinished = true;
        }

        bool InitializeFinished { get; set; }

        public void StartWatch()
        {
            FileSystemWatcher = FilesWatcherHelper.StartWatch(config.MonitorFolder, OnFileChange, RenamedEventHandler);

            Task.Run(() =>
            {
                RetryAllFailed(tokenSource.Token);
            }, tokenSource.Token);

            Task.Run(() =>
            {
                SaveLuceneResultsWhenNeeded(tokenSource.Token);
            }, tokenSource.Token);

            log?.Info($"Start monitoring files change on {config.MonitorFolder}");
        }

        public void Dispose()
        {
            FileSystemWatcher.EnableRaisingEvents = false;
            FileSystemWatcher.Dispose();
            tokenSource.Cancel();
            LucenePool.SaveResultsAndClearLucenePool(config);
        }

        FileSystemWatcher FileSystemWatcher { get; set; }
        const int WaitMilliseconds = 100;

        CodeIndexConfiguration config;
        string[] excludedExtensions;
        string[] excludedPaths;
        int saveIntervalSeconds;
        string[] includedExtensions;
        ILog log;
        CancellationTokenSource tokenSource;

        void OnFileChange(object sender, FileSystemEventArgs e)
        {
            log?.Info($"File Change - ChangeType: {e.ChangeType} FullPath: {e.FullPath} Name: {e.Name}");

            if (!IsExcludedFromIndex(e))
            {
                switch (e.ChangeType)
                {
                    case WatcherChangeTypes.Changed:
                        if (InitializeFinished)
                        {
                            UpdateIndex(e.FullPath);
                        }
                        else
                        {
                            AddFileChangesToRetrySouce(e.FullPath, WatcherChangeTypes.Changed, null);
                        }
                        break;

                    case WatcherChangeTypes.Created:
                        if (InitializeFinished)
                        {
                            CreateNewIndex(e.FullPath);
                        }
                        else
                        {
                            AddFileChangesToRetrySouce(e.FullPath, WatcherChangeTypes.Created, null);
                        }
                        break;

                    case WatcherChangeTypes.Deleted:
                        DeleteIndex(e.FullPath);
                        break;
                }
            }
        }

        void RenamedEventHandler(object sender, RenamedEventArgs e)
        {
            log?.Info($"File Renamed - ChangeType: {e.ChangeType} FullPath: {e.FullPath} Name: {e.Name} OldFullPath: {e.OldFullPath} OldName: {e.OldName}");

            if (!IsExcludedFromIndex(e))
            {
                switch (e.ChangeType)
                {
                    case WatcherChangeTypes.Renamed:
                        if (InitializeFinished)
                        {
                            FileRenamed(e.OldFullPath, e.FullPath);
                        }
                        else
                        {
                            AddFileChangesToRetrySouce(e.FullPath, WatcherChangeTypes.Created, null, e.OldFullPath);
                        }
                        break;
                }
            }
            else
            {
                DeleteAllDocumentsIndexUnder(e.OldFullPath);
            }
        }

        bool IsExcludedFromIndex(FileSystemEventArgs e)
        {
            var excluded = excludedPaths.Any(u => e.FullPath.ToUpperInvariant().Contains(u))
                    || excludedExtensions.Any(u => e.FullPath.EndsWith(u, StringComparison.InvariantCultureIgnoreCase))
                    || includedExtensions.Length > 0 && !includedExtensions.Any(u => e.FullPath.EndsWith(u, StringComparison.InvariantCultureIgnoreCase));

            if (excluded)
            {
                log?.Info($"{e.FullPath} is excluded from index");
            }

            return excluded;
        }

        void CreateNewIndex(string fullPath, PendingRetrySource pendingRetrySource = null)
        {
            if (IsFile(fullPath))
            {
                var fileInfo = new FileInfo(fullPath);
                try
                {
                    Thread.Sleep(WaitMilliseconds); // Wait to let file finished write to disk

                    if (fileInfo.Exists)
                    {
                        var content = FilesContentHelper.ReadAllText(fullPath);
                        CodeIndexBuilder.BuildIndex(config, false, false, false, new[] { CodeSource.GetCodeSource(fileInfo, content) });
                        WordsHintBuilder.UpdateWordsAndUpdateIndex(config, WordSegmenter.GetWords(content), log);
                        pendingChanges++;
                    }
                }
                catch (IOException)
                {
                    AddFileChangesToRetrySouce(fullPath, WatcherChangeTypes.Created, pendingRetrySource);
                }
                catch (Exception ex)
                {
                    log?.Error(ex.ToString());
                }
            }
        }

        void UpdateIndex(string fullPath, PendingRetrySource pendingRetrySource = null)
        {
            if (IsFile(fullPath))
            {
                var fileInfo = new FileInfo(fullPath);
                try
                {
                    Thread.Sleep(WaitMilliseconds); // Wait to let file finished write to disk

                    if (fileInfo.Exists)
                    {
                        var content = FilesContentHelper.ReadAllText(fullPath);
                        var document = CodeIndexBuilder.GetDocumentFromSource(CodeSource.GetCodeSource(fileInfo, content));
                        CodeIndexBuilder.UpdateIndex(config.LuceneIndexForCode, GetNoneTokenizeFieldTerm(nameof(CodeSource.FilePath), fullPath), document);
                        WordsHintBuilder.UpdateWordsAndUpdateIndex(config, WordSegmenter.GetWords(content), log);
                        pendingChanges++;
                    }
                }
                catch (IOException)
                {
                    AddFileChangesToRetrySouce(fullPath, WatcherChangeTypes.Changed, pendingRetrySource);
                }
                catch (Exception ex)
                {
                    log?.Error(ex.ToString());
                }
            }
        }

        void DeleteIndex(string fullPath)
        {
            try
            {
                CodeIndexBuilder.DeleteIndex(config.LuceneIndexForCode, GetNoneTokenizeFieldTerm(nameof(CodeSource.FilePath), fullPath));
                pendingChanges++;
            }
            catch (Exception ex)
            {
                log?.Error(ex.ToString());
            }
        }

        void DeleteAllDocumentsIndexUnder(string forderOldFullPath)
        {
            try
            {
                var term = new PrefixQuery(GetNoneTokenizeFieldTerm(nameof(CodeSource.FilePath), forderOldFullPath));
                CodeIndexBuilder.DeleteIndex(config.LuceneIndexForCode, term);
                pendingChanges++;
            }
            catch (Exception ex)
            {
                log?.Error(ex.ToString());
            }
        }

        void FileRenamed(string oldFullPath, string fullPath, PendingRetrySource pendingRetrySource = null)
        {
            try
            {
                if (IsFile(fullPath))
                {
                    var fileInfo = new FileInfo(fullPath);

                    try
                    {
                        if (fileInfo.Exists)
                        {
                            var content = FilesContentHelper.ReadAllText(fullPath);
                            var document = CodeIndexBuilder.GetDocumentFromSource(CodeSource.GetCodeSource(fileInfo, content));
                            CodeIndexBuilder.UpdateIndex(config.LuceneIndexForCode, GetNoneTokenizeFieldTerm(nameof(CodeSource.FilePath), oldFullPath), document);
                            pendingChanges++;
                        }
                    }
                    catch (IOException)
                    {
                        AddFileChangesToRetrySouce(fullPath, WatcherChangeTypes.Renamed, pendingRetrySource, oldFullPath);
                    }
                }
                else if (IsDirectory(fullPath))
                {
                    // Rebuild All Sub Directory Index File, rename the index path
                    var term = new PrefixQuery(GetNoneTokenizeFieldTerm(nameof(CodeSource.FilePath), oldFullPath));
                    var docs = CodeIndexSearcher.Search(config.LuceneIndexForCode, term, int.MaxValue);
                    foreach (var doc in docs)
                    {
                        CodeIndexBuilder.UpdateCodeFilePath(doc, oldFullPath, fullPath);
                        CodeIndexBuilder.UpdateIndex(config.LuceneIndexForCode, new Term(nameof(CodeSource.CodePK), doc.Get(nameof(CodeSource.CodePK))), doc);
                        pendingChanges++;
                    }
                }
            }
            catch (Exception ex)
            {
                log?.Error(ex.ToString());
            }
        }

        void AddFileChangesToRetrySouce(string fullPath, WatcherChangeTypes changesType, PendingRetrySource pendingRetrySource, string oldFullPath = null)
        {
            if (pendingRetrySource == null)
            {
                pendingRetrySource = new PendingRetrySource
                {
                    FilePath = fullPath,
                    LastRetryUTCDate = DateTime.UtcNow,
                    ChangesType = changesType,
                    OldPath = oldFullPath
                };
            }
            else
            {
                pendingRetrySource.RetryTimes++;
                pendingRetrySource.LastRetryUTCDate = DateTime.UtcNow;
            }

            pendingRetryCodeSources.Enqueue(pendingRetrySource);
        }

        protected ConcurrentQueue<PendingRetrySource> pendingRetryCodeSources = new ConcurrentQueue<PendingRetrySource>();

        bool IsFile(string path)
        {
            return File.Exists(path);
        }

        bool IsDirectory(string path)
        {
            return Directory.Exists(path);
        }

        void RetryAllFailed(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if (InitializeFinished && pendingRetryCodeSources.TryDequeue(out var pendingRetrySource))
                {
                    if (pendingRetrySource.RetryTimes <= 10) // Always Failed, Stop Retry
                    {
                        log?.Info($"Retry failed - ChangesType: {pendingRetrySource.ChangesType} FilePath:{pendingRetrySource.FilePath} LastRetryUTCDate: {pendingRetrySource.LastRetryUTCDate.ToString("yyyyMMddHHmmssfff")} OldPath: {pendingRetrySource.OldPath} RetryTimes: {pendingRetrySource.RetryTimes}");

                        Task.Run(() =>
                        {
                            if (pendingRetrySource.LastRetryUTCDate > DateTime.UtcNow.AddSeconds(-5)) // Failed In 5 Seconds
                            {
                                Thread.Sleep(5000);
                            }

                            switch (pendingRetrySource.ChangesType)
                            {
                                case WatcherChangeTypes.Changed:
                                    UpdateIndex(pendingRetrySource.FilePath, pendingRetrySource);
                                    break;

                                case WatcherChangeTypes.Created:
                                    CreateNewIndex(pendingRetrySource.FilePath, pendingRetrySource);
                                    break;

                                case WatcherChangeTypes.Renamed:
                                    FileRenamed(pendingRetrySource.FilePath, pendingRetrySource.OldPath, pendingRetrySource);
                                    break;
                            }
                        }, cancellationToken);
                    }
                    else
                    {
                        log?.Warn($"Stop retry failed - ChangesType: {pendingRetrySource.ChangesType} FilePath:{pendingRetrySource.FilePath} LastRetryUTCDate: {pendingRetrySource.LastRetryUTCDate.ToString("yyyyMMddHHmmssfff")} OldPath: {pendingRetrySource.OldPath} RetryTimes: {pendingRetrySource.RetryTimes}");
                    }
                }
                else
                {
                    Thread.Sleep(SleepMilliseconds); // Sleep when nothing need to requeue
                }
            }
        }

        protected virtual int SleepMilliseconds => 10000;

        int pendingChanges = 0;
        DateTime lastSaveDate = DateTime.UtcNow;

        void SaveLuceneResultsWhenNeeded(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                token.ThrowIfCancellationRequested();

                if (InitializeFinished && (pendingChanges > 100 || (DateTime.UtcNow - lastSaveDate).Seconds >= saveIntervalSeconds))
                {
                    pendingChanges = 0;
                    LucenePool.SaveResultsAndClearLucenePool(config);
                    lastSaveDate = DateTime.UtcNow;
                    log?.Info($"Save all pending changes successful");
                }
                else
                {
                    Thread.Sleep(saveIntervalSeconds * 100); //  Sleep when nothing need to save
                }
            }
        }

        public static Term GetNoneTokenizeFieldTerm(string fieldName, string termValue)
        {
            return new Term($"{fieldName}{Constants.NoneTokenizeFieldSuffix}", termValue);
        }
    }
}
