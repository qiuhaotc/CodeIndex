using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CodeIndex.Common;
using CodeIndex.Files;
using CodeIndex.IndexBuilder;
using CodeIndex.Search;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;

namespace CodeIndex.MaintainIndex
{
    public class CodeFilesIndexMaintainer : IDisposable
    {
        public CodeFilesIndexMaintainer(string watchPath, string luceneIndex, string[] excludedExtensions, string[] excludedPaths, int saveIntervalSeconds = 300, string[] includedExtensions = null, ILog log = null)
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

            Task.Run(() =>
            {
                RetryAllFailed(tokenSource.Token);
            }, tokenSource.Token);

            Task.Run(() =>
            {
                SaveLuceneResultsWhenNeeded(tokenSource.Token);
            }, tokenSource.Token);
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
                        CodeIndexBuilder.DeleteIndex(luceneIndex, new Term(nameof(CodeSource.FilePath), e.FullPath));
                        break;
                }

                pendingChanges++;
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

                pendingChanges++;
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
                        var content = File.ReadAllText(fullPath, FilesEncodingHelper.GetEncoding(fullPath));
                        CodeIndexBuilder.BuildIndex(luceneIndex, false, false, false, new[] { CodeSource.GetCodeSource(fileInfo, content) });
                    }
                }
                catch (IOException)
                {
                    HandleFileLoadException(fullPath, WatcherChangeTypes.Created, pendingRetrySource);
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
                        var content = File.ReadAllText(fullPath, FilesEncodingHelper.GetEncoding(fullPath));
                        // TODO: When Date Not Change, Not Update
                        var document = CodeIndexBuilder.GetDocumentFromSource(CodeSource.GetCodeSource(fileInfo, content));
                        CodeIndexBuilder.UpdateIndex(luceneIndex, new Term(nameof(CodeSource.FilePath), fullPath), document);
                    }
                }
                catch (IOException)
                {
                    HandleFileLoadException(fullPath, WatcherChangeTypes.Changed, pendingRetrySource);
                }
            }
        }

        void FileRenamed(string oldFullPath, string fullPath, PendingRetrySource pendingRetrySource = null)
        {
            if (IsFile(fullPath))
            {
                CodeIndexBuilder.DeleteIndex(luceneIndex, new Term(nameof(CodeSource.FilePath), oldFullPath));

                var fileInfo = new FileInfo(fullPath);
                try
                {
                    if (fileInfo.Exists)
                    {
                        var content = File.ReadAllText(fullPath, FilesEncodingHelper.GetEncoding(fullPath));
                        var document = CodeIndexBuilder.GetDocumentFromSource(CodeSource.GetCodeSource(fileInfo, content));
                        // TODO: When Date Not Change, Not Update
                        CodeIndexBuilder.UpdateIndex(luceneIndex, new Term(nameof(CodeSource.FilePath), oldFullPath), document);
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
                var term = new PrefixQuery(new Term(nameof(CodeSource.FilePath), oldFullPath));
                var files = CodeIndexSearcher.Search(luceneIndex, term, int.MaxValue);
                foreach (var file in files)
                {
                    var pathField = file.Get(nameof(CodeSource.FilePath));
                    file.RemoveField(nameof(CodeSource.FilePath));
                    file.Add(new StringField(nameof(CodeSource.FilePath), pathField.Replace(oldFullPath, fullPath), Field.Store.YES));
                    CodeIndexBuilder.UpdateIndex(luceneIndex, new Term(nameof(CodeSource.CodePK), file.Get(nameof(CodeSource.CodePK))), file);
                }
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
                    LucenePool.SaveResultsAndClearLucenePool(luceneIndex);
                    pendingChanges = 0;
                    lastSaveDate = DateTime.UtcNow;
                }
                else
                {
                    Thread.Sleep(saveIntervalSeconds * 100); //  Sleep when nothing need to save
                }
            }
        }
    }
}
