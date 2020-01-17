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
        public CodeFilesIndexMaintainer(string watchPath, string indexPath, string[] excludedExtensions, string[] excludedPaths)
        {
            watchPath.RequireNotNullOrEmpty(nameof(watchPath));
            excludedExtensions.RequireNotNull(nameof(watchPath));
            excludedPaths.RequireNotNull(nameof(excludedPaths));

            this.indexPath = indexPath;
            this.excludedExtensions = excludedExtensions;
            this.excludedPaths = excludedPaths;
            FileSystemWatcher = FilesWatcherHelper.StartWatch(watchPath, OnFileChange, RenamedEventHandler);
            tokenSource = new CancellationTokenSource();

            Task.Run(() =>
            {
                RetryAllFailed(tokenSource.Token);
            }, tokenSource.Token);
        }

        public void Dispose()
        {
            FileSystemWatcher.EnableRaisingEvents = false;
            FileSystemWatcher.Dispose();
            tokenSource.Cancel();
        }

        public FileSystemWatcher FileSystemWatcher { get; private set; }
        const int WaitMilliseconds = 100;

        string indexPath;
        string[] excludedExtensions;
        string[] excludedPaths;
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
                        CodeIndexBuilder.DeleteIndex(indexPath, new Term(nameof(CodeSource.FilePath), e.FullPath));
                        break;
                }

                CodeIndexBuilder.CloseIndexWriterAndCommitChange(indexPath);
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

                CodeIndexBuilder.CloseIndexWriterAndCommitChange(indexPath);
            }
        }

        bool IsExcludedFromIndex(FileSystemEventArgs e)
        {
            return excludedPaths.Any(u => e.FullPath.Contains(u)) || excludedExtensions.Any(u => e.FullPath.EndsWith(u));
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
                        CodeIndexBuilder.BuildIndex(indexPath, false, false, CodeSource.GetCodeSource(fileInfo, content));
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
                        CodeIndexBuilder.UpdateIndex(indexPath, new Term(nameof(CodeSource.FilePath), fullPath), document);
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
                CodeIndexBuilder.DeleteIndex(indexPath, new Term(nameof(CodeSource.FilePath), oldFullPath));

                var fileInfo = new FileInfo(fullPath);
                try
                {
                    if (fileInfo.Exists)
                    {
                        var content = File.ReadAllText(fullPath, FilesEncodingHelper.GetEncoding(fullPath));
                        // TODO: When Date Not Change, Not Update
                        CodeIndexBuilder.BuildIndex(indexPath, false, false, CodeSource.GetCodeSource(fileInfo, content));
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

            pendingRetryCodeSource.Enqueue(pendingRetrySource);
        }

        ConcurrentQueue<PendingRetrySource> pendingRetryCodeSource = new ConcurrentQueue<PendingRetrySource>();

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
                if (pendingRetryCodeSource.TryDequeue(out var pendingRetrySource))
                {
                    if (pendingRetrySource.RetryTimes <= 10) // Always Failed, Stop Retry
                    {
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
                    }
                }
                else
                {
                    Thread.Sleep(10000); // Sleep 10 seconds when nothing need to requeue
                }
            }
        }
    }
}
