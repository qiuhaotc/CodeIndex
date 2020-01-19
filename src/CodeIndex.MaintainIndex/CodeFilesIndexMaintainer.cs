using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CodeIndex.Common;
using CodeIndex.Files;
using CodeIndex.IndexBuilder;
using Lucene.Net.Index;

namespace CodeIndex.MaintainIndex
{
    public class CodeFilesIndexMaintainer : IDisposable
    {
        public CodeFilesIndexMaintainer(string watchPath, string luceneIndex, string[] excludedExtensions, string[] excludedPaths, int saveIntervalSeconds = 300, string[] includedExtensions = null)
        {
            watchPath.RequireNotNullOrEmpty(nameof(watchPath));
            excludedExtensions.RequireNotNull(nameof(watchPath));
            excludedPaths.RequireNotNull(nameof(excludedPaths));
            saveIntervalSeconds.RequireRange(nameof(saveIntervalSeconds), 3600, 1);

            this.luceneIndex = luceneIndex;
            this.excludedExtensions = excludedExtensions;
            this.excludedPaths = excludedPaths;
            this.saveIntervalSeconds = saveIntervalSeconds;
            this.includedExtensions = includedExtensions;
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
            LucenePool.SaveLuceneResultsAndCloseIndexWriter(luceneIndex);
        }

        FileSystemWatcher FileSystemWatcher { get; set; }
        const int WaitMilliseconds = 100;

        string luceneIndex;
        string[] excludedExtensions;
        string[] excludedPaths;
        int saveIntervalSeconds;
        string[] includedExtensions;
        CancellationTokenSource tokenSource;

        void OnFileChange(object sender, FileSystemEventArgs e)
        {
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
			return excludedPaths.Any(u => e.FullPath.ToUpperInvariant().Contains(u)) || excludedExtensions.Any(u => e.FullPath.EndsWith(u, StringComparison.InvariantCultureIgnoreCase)) || includedExtensions != null && !includedExtensions.Any(u => e.FullPath.EndsWith(u, StringComparison.InvariantCultureIgnoreCase));
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
                        // TODO: When Date Not Change, Not Update
                        CodeIndexBuilder.BuildIndex(luceneIndex, false, false, false, new[] { CodeSource.GetCodeSource(fileInfo, content) });
                    }
                }
                catch (IOException)
                {
                    HandleFileLoadException(fullPath, WatcherChangeTypes.Renamed, pendingRetrySource, oldFullPath);
                }
            }
            else if (IsDirectory(fullPath))
            {
                // TODO: Rebuild All Sub Directory Index File, maybe just rename the index path
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

                    // TODO: Add Log
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
                    LucenePool.SaveLuceneResultsAndCloseIndexWriter(luceneIndex);
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
