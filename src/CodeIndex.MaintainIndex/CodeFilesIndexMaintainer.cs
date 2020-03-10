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
        public CodeFilesIndexMaintainer(string watchPath, string luceneIndex, string[] excludedExtensions, string[] excludedPaths, int saveIntervalSeconds = 300, string[] includedExtensions = null, ILog log = null, List<FileInfo> initalizeFailedFiles = null)
        {
            watchPath.RequireNotNullOrEmpty(nameof(watchPath));
            excludedExtensions.RequireNotNull(nameof(watchPath));
            excludedPaths.RequireNotNull(nameof(excludedPaths));
            saveIntervalSeconds.RequireRange(nameof(saveIntervalSeconds), 3600, 1);

            this.luceneIndex = luceneIndex;
            this.excludedExtensions = excludedExtensions.Select(u => u.ToUpperInvariant()).ToArray();
            this.excludedPaths = excludedPaths.Select(u => u.ToUpperInvariant()).ToArray();
            this.saveIntervalSeconds = saveIntervalSeconds;
            this.includedExtensions = includedExtensions?.Select(u => u.ToUpperInvariant()).ToArray();
            this.log = log;
            FileSystemWatcher = FilesWatcherHelper.StartWatch(watchPath, OnFileChange, RenamedEventHandler);
            tokenSource = new CancellationTokenSource();

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

            Task.Run(() =>
            {
                RetryAllFailed(tokenSource.Token);
            }, tokenSource.Token);

            Task.Run(() =>
            {
                SaveLuceneResultsWhenNeeded(tokenSource.Token);
            }, tokenSource.Token);

            log?.Info("Start monitoring files change");
        }

        public void Dispose()
        {
            FileSystemWatcher.EnableRaisingEvents = false;
            FileSystemWatcher.Dispose();
            tokenSource.Cancel();
            LucenePool.SaveResultsAndClearLucenePool(luceneIndex);
        }

        // TODO: Add a boolean field to determine initialize is finished
        FileSystemWatcher FileSystemWatcher { get; set; }
        const int WaitMilliseconds = 100;

        string luceneIndex;
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
                        UpdateIndex(e.FullPath);
                        break;

                    case WatcherChangeTypes.Created:
                        CreateNewIndex(e.FullPath);
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
                        FileRenamed(e.OldFullPath, e.FullPath);
                        break;
                }
            }
        }

        bool IsExcludedFromIndex(FileSystemEventArgs e)
        {
            var excluded = true;

            if (IsFile(e.FullPath))
            {
                excluded = excludedPaths.Any(u => e.FullPath.ToUpperInvariant().Contains(u))
                    || excludedExtensions.Any(u => e.FullPath.EndsWith(u, StringComparison.InvariantCultureIgnoreCase))
                    || includedExtensions != null && !includedExtensions.Any(u => e.FullPath.EndsWith(u, StringComparison.InvariantCultureIgnoreCase));
            }
            else if (IsDirectory(e.FullPath))
            {
                excluded = excludedPaths.Any(u => e.FullPath.ToUpperInvariant().Contains(u));
            }

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
                        CodeIndexBuilder.BuildIndex(luceneIndex, false, false, false, new[] { CodeSource.GetCodeSource(fileInfo, content) });
                        pendingChanges++;
                    }
                }
                catch (IOException)
                {
                    HandleFileLoadException(fullPath, WatcherChangeTypes.Created, pendingRetrySource);
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
                        // TODO: When Date Not Change, Not Update
                        var document = CodeIndexBuilder.GetDocumentFromSource(CodeSource.GetCodeSource(fileInfo, content));
                        CodeIndexBuilder.UpdateIndex(luceneIndex, GetNoneTokenizeFieldTerm(nameof(CodeSource.FilePath), fullPath), document);
                        pendingChanges++;
                    }
                }
                catch (IOException)
                {
                    HandleFileLoadException(fullPath, WatcherChangeTypes.Changed, pendingRetrySource);
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
                CodeIndexBuilder.DeleteIndex(luceneIndex, GetNoneTokenizeFieldTerm(nameof(CodeSource.FilePath), fullPath));
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
                            CodeIndexBuilder.UpdateIndex(luceneIndex, GetNoneTokenizeFieldTerm(nameof(CodeSource.FilePath), oldFullPath), document);
                            pendingChanges++;
                        }
                    }
                    catch (IOException)
                    {
                        HandleFileLoadException(fullPath, WatcherChangeTypes.Renamed, pendingRetrySource, oldFullPath);
                    }
                }
                else if (IsDirectory(fullPath))
                {
                    // Rebuild All Sub Directory Index File, rename the index path
                    var term = new PrefixQuery(GetNoneTokenizeFieldTerm(nameof(CodeSource.FilePath), oldFullPath));
                    var docs = CodeIndexSearcher.Search(luceneIndex, term, int.MaxValue);
                    foreach (var doc in docs)
                    {
                        CodeIndexBuilder.UpdateCodeFilePath(doc, oldFullPath, fullPath);
                        CodeIndexBuilder.UpdateIndex(luceneIndex, new Term(nameof(CodeSource.CodePK), doc.Get(nameof(CodeSource.CodePK))), doc);
                        pendingChanges++;
                    }
                }
            }
            catch (Exception ex)
            {
                log?.Error(ex.ToString());
            }
        }

        void HandleFileLoadException(string fullPath, WatcherChangeTypes changesType, PendingRetrySource pendingRetrySource, string oldFullPath = null)
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

        ConcurrentQueue<PendingRetrySource> pendingRetryCodeSources = new ConcurrentQueue<PendingRetrySource>();

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
                if (pendingRetryCodeSources.TryDequeue(out var pendingRetrySource))
                {
                    if (pendingRetrySource.RetryTimes <= 10) // Always Failed, Stop Retry
                    {
                        log?.Info($"Retry failed - ChangesType: {pendingRetrySource.ChangesType} FilePath:{pendingRetrySource.FilePath} LastRetryUTCDate: {pendingRetrySource.LastRetryUTCDate.ToString("yyyyMMddHHmmssfff")} OldPath: {pendingRetrySource.OldPath} RetryTimes: {pendingRetrySource.RetryTimes}");

                        Task.Run(() =>
                        {
                            if (pendingRetrySource.LastRetryUTCDate > DateTime.UtcNow.AddSeconds(-10)) // Failed In 10 Seconds
                            {
                                Thread.Sleep(10000);
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
                    Thread.Sleep(10000); // Sleep 10 seconds when nothing need to requeue
                }
            }
        }

        int pendingChanges = 0;
        DateTime lastSaveDate = DateTime.UtcNow;

        void SaveLuceneResultsWhenNeeded(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                token.ThrowIfCancellationRequested();

                if (pendingChanges > 100 || (DateTime.UtcNow - lastSaveDate).Seconds >= saveIntervalSeconds)
                {
                    pendingChanges = 0;
                    LucenePool.SaveResultsAndClearLucenePool(luceneIndex);
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
