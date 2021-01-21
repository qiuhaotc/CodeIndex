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
using Microsoft.Extensions.Logging;

namespace CodeIndex.MaintainIndex
{
    public class IndexMaintainer : IDisposable
    {
        public IndexMaintainer(IndexConfig indexConfig, CodeIndexConfiguration codeIndexConfiguration, ILogger log)
        {
            indexConfig.RequireNotNull(nameof(indexConfig));
            codeIndexConfiguration.RequireNotNull(nameof(codeIndexConfiguration));
            log.RequireNotNull(nameof(log));

            IndexConfig = indexConfig;
            CodeIndexConfiguration = codeIndexConfiguration;
            Log = log;
            Status = IndexStatus.Idle;

            ExcludedExtensions = indexConfig.ExcludedExtensionsArray.Select(u => u.ToUpperInvariant()).ToArray();
            ExcludedPaths = FilePathHelper.GetPaths(indexConfig.ExcludedPathsArray, codeIndexConfiguration.IsInLinux);
            IncludedExtensions = indexConfig.IncludedExtensionsArray.Select(u => u.ToUpperInvariant()).ToArray() ?? Array.Empty<string>();
            TokenSource = new CancellationTokenSource();
        }

        public async Task InitializeIndex(bool forceRebuild)
        {
            if (Status != IndexStatus.Idle)
            {
                return;
            }

            try
            {
                Log.LogInformation($"{IndexConfig.IndexName}: Start Initializing");
                Status = IndexStatus.Initializing;

                if (Directory.Exists(IndexConfig.MonitorFolder))
                {
                    await Task.Run(() =>
                    {
                        InitializeIndexCore(forceRebuild);
                    }, TokenSource.Token);

                    Status = IndexStatus.Initialized;
                }
                else
                {
                    Status = IndexStatus.Error;
                    Description = "Monitor Folder Not Exist";
                    Log.LogError($"{IndexConfig.IndexName}: Initializing failed: {Description}");
                }
            }
            catch (Exception ex)
            {
                Status = IndexStatus.Error;
                Description = ex.Message;
                Log.LogError($"{IndexConfig.IndexName}: Initializing failed: {ex}");
            }
        }

        public async Task MaintainIndexes()
        {
            if (Status != IndexStatus.Initialized)
            {
                return;
            }

            Log.LogInformation($"{IndexConfig.IndexName}: Start Maintain Indexes");

            Status = IndexStatus.Monitoring;

            await MaintainIndexesCore();
        }

        async Task MaintainIndexesCore()
        {
            FetchIntervalSeconds.RequireRange(nameof(FetchIntervalSeconds), 3, 1);

            while (!TokenSource.Token.IsCancellationRequested)
            {
                TokenSource.Token.ThrowIfCancellationRequested();

                var fetchEndDate = DateTime.UtcNow.AddSeconds(-FetchIntervalSeconds * 2);
                var notChangedDuring = fetchEndDate.AddSeconds(FetchIntervalSeconds);
                if (ChangedSources.Count(u => u.ChangedUTCDate > fetchEndDate && u.ChangedUTCDate <= notChangedDuring) == 0)
                {
                    var orderedNeedProcessingChanges = ChangedSources.Where(u => u.ChangedUTCDate <= fetchEndDate).ToList();
                    if (orderedNeedProcessingChanges.Count > 0)
                    {
                        foreach (var _ in orderedNeedProcessingChanges)
                        {
                            ChangedSources.TryDequeue(out var _);
                        }

                        ProcessingChanges(orderedNeedProcessingChanges);

                        IndexBuilder.Commit();

                        TriggerCommitFinished();
                    }
                }

                TokenSource.Token.ThrowIfCancellationRequested();

                var fetchRetryEndDate = DateTime.UtcNow.AddSeconds(-3);
                var needRetry = PendingRetryCodeSources.Count(u => u.LastRetryUTCDate <= fetchRetryEndDate);
                if (needRetry > 0)
                {
                    var needRetrySources = new List<ChangedSource>();
                    for (var index = 0; index < needRetry; index++)
                    {
                        if (PendingRetryCodeSources.TryDequeue(out var pendingRetrySource))
                        {
                            needRetrySources.Add(pendingRetrySource);
                        }
                    }

                    ProcessingChanges(needRetrySources.OrderBy(u => u.ChangedUTCDate).ToList(), true);

                    IndexBuilder.Commit();
                }

                await Task.Delay(FetchIntervalSeconds * 1000, TokenSource.Token);
            }
        }

        protected virtual void TriggerCommitFinished()
        {
        }

        void ProcessingChanges(IList<ChangedSource> orderedNeedProcessingChanges, bool isFailedChangedSource = false)
        {
            var prefix = isFailedChangedSource ? "Failed " : string.Empty;

            orderedNeedProcessingChanges.PreProcessingChanges(prefix, IndexConfig, u => Log.LogInformation(u));

            Log.LogInformation($"{IndexConfig.IndexName}: Processing {prefix}Changes start, changes count: {orderedNeedProcessingChanges.Count}");

            foreach (var changes in orderedNeedProcessingChanges)
            {
                TokenSource.Token.ThrowIfCancellationRequested();

                switch (changes.ChangesType)
                {
                    case WatcherChangeTypes.Changed:
                        UpdateIndex(changes);
                        break;

                    case WatcherChangeTypes.Created:
                        CreateIndex(changes);
                        break;

                    case WatcherChangeTypes.Deleted:
                        DeleteIndex(changes);
                        break;

                    case WatcherChangeTypes.Renamed:
                        RenameIndex(changes);
                        break;

                    default:
                        Log.LogError($"{IndexConfig.IndexName}: Unknown changes type {changes}");
                        break;
                }

                Log.LogInformation($"{IndexConfig.IndexName}: Processing {changes} finished");
            }

            Log.LogInformation($"{IndexConfig.IndexName}: Processing {prefix}Changes finished");
        }

        void CreateIndex(ChangedSource changes)
        {
            if (IsFile(changes.FilePath))
            {
                if (IndexBuilder.CreateIndex(new FileInfo(changes.FilePath)) == IndexBuildResults.FailedWithIOException)
                {
                    EnqueueToFailedSource(changes);
                }
            }
            else if (IsDirectory(changes.FilePath))
            {
                foreach (var file in Directory.GetFiles(changes.FilePath, "*", SearchOption.AllDirectories).Where(
                    file => ChangedSources.All(changedSource => !changedSource.FilePath.Equals(file, StringComparison.InvariantCultureIgnoreCase))
                    ))
                {
                    Log.LogInformation($"{IndexConfig.IndexName}: Enqueue File {file} Created to changes source");
                    EnqueueChangeSource(WatcherChangeTypes.Created, file);
                }
            }
        }

        void RenameIndex(ChangedSource changes)
        {
            if (IsExcludedFromIndex(changes.FilePath))
            {
                if (!IsExcludedFromIndex(changes.OldPath))
                {
                    IndexBuilder.DeleteIndex(changes.OldPath);
                }
            }
            else
            {
                if (IsFile(changes.FilePath))
                {
                    if (IndexBuilder.RenameFileIndex(changes.OldPath, changes.FilePath) == IndexBuildResults.FailedWithIOException)
                    {
                        EnqueueToFailedSource(changes);
                    }
                }
                else if (IsDirectory(changes.FilePath))
                {
                    IndexBuilder.RenameFolderIndexes(changes.OldPath, changes.FilePath, TokenSource.Token);
                }
            }
        }

        void EnqueueToFailedSource(ChangedSource changes)
        {
            if (changes is not PendingRetrySource)
            {
                Log.LogError($"Enqueue failed processing changed source {changes}"); PendingRetryCodeSources.Enqueue(new PendingRetrySource
                {
                    ChangesType = changes.ChangesType,
                    FilePath = changes.FilePath,
                    OldPath = changes.FilePath,
                    ChangedUTCDate = changes.ChangedUTCDate,
                    LastRetryUTCDate = DateTime.UtcNow
                });
            }
        }

        bool IsFile(string path)
        {
            return File.Exists(path);
        }

        bool IsDirectory(string path)
        {
            return Directory.Exists(path);
        }

        void DeleteIndex(ChangedSource changes)
        {
            IndexBuilder.DeleteIndex(changes.FilePath);
        }

        void UpdateIndex(ChangedSource changes)
        {
            if (IsFile(changes.FilePath))
            {
                if (IndexBuilder.UpdateIndex(new FileInfo(changes.FilePath), TokenSource.Token) == IndexBuildResults.FailedWithIOException)
                {
                    EnqueueToFailedSource(changes);
                }
            }
        }

        void InitializeIndexCore(bool forceRebuild)
        {
            var folders = IndexConfig.GetFolders(CodeIndexConfiguration.LuceneIndex);

            IndexBuilder = new CodeIndexBuilder(
                IndexConfig.IndexName,
                new LucenePoolLight(folders.CodeIndexFolder),
                new LucenePoolLight(folders.HintIndexFolder),
                Log);

            IndexBuilder.InitIndexFolderIfNeeded();

            Status = IndexStatus.Initializing_ComponentInitializeFinished;

            ChangedSources = new ConcurrentQueue<ChangedSource>();
            PendingRetryCodeSources = new ConcurrentQueue<PendingRetrySource>();
            FilesWatcher = FilesWatcherHelper.StartWatch(IndexConfig.MonitorFolder, OnChange, OnRename);
            Log.LogInformation($"{IndexConfig.IndexName}: Start Watch files change");

            var allFiles = FilesFetcher.FetchAllFiles(IndexConfig.MonitorFolder, IndexConfig.ExcludedExtensionsArray, IndexConfig.ExcludedPathsArray, includedExtensions: IndexConfig.IncludedExtensionsArray, isInLinux: CodeIndexConfiguration.IsInLinux).ToList();
            Log.LogInformation($"{IndexConfig.IndexName}: Fetching {allFiles.Count} files need to indexing");

            List<FileInfo> needToBuildIndex = null;
            var failedUpdateOrDeleteFiles = new List<string>();
            var brandNewBuild = false;

            if (IndexBuilderHelper.IndexExists(IndexBuilder.CodeIndexPool.LuceneIndex))
            {
                if (forceRebuild)
                {
                    brandNewBuild = true;
                    Log.LogInformation($"{IndexConfig.IndexName}: Force rebuild all indexes");
                    IndexBuilder.DeleteAllIndex();
                }
                else
                {
                    Log.LogInformation($"{IndexConfig.IndexName}: Compare index difference");

                    var allCodeSource = IndexBuilder.GetAllIndexedCodeSource();

                    GC.Collect(); // Run GC after fetching massive documents from index

                    needToBuildIndex = new List<FileInfo>();
                    var allFilesDictionary = allFiles.ToDictionary(u => u.FullName);

                    foreach (var codeSource in allCodeSource)
                    {
                        TokenSource.Token.ThrowIfCancellationRequested();

                        if (allFilesDictionary.TryGetValue(codeSource.FilePath, out var fileInfo))
                        {
                            if (fileInfo.LastWriteTimeUtc != codeSource.LastWriteTimeUtc)
                            {
                                Log.LogInformation($"{IndexConfig.IndexName}: File {fileInfo.FullName} modified");
                                if (IndexBuilder.UpdateIndex(fileInfo, TokenSource.Token) != IndexBuildResults.Successful)
                                {
                                    failedUpdateOrDeleteFiles.Add(codeSource.FilePath);
                                }
                            }

                            allFilesDictionary.Remove(codeSource.FilePath);
                        }
                        else
                        {
                            Log.LogInformation($"{IndexConfig.IndexName}: File {codeSource.FilePath} deleted");
                            if (!IndexBuilder.DeleteIndex(codeSource.FilePath))
                            {
                                failedUpdateOrDeleteFiles.Add(codeSource.FilePath);
                            }
                        }
                    }

                    foreach (var needToCreateFiles in allFilesDictionary)
                    {
                        Log.LogInformation($"{IndexConfig.IndexName}: Found new file {needToCreateFiles.Value.FullName}");
                        needToBuildIndex.Add(needToCreateFiles.Value);
                    }
                }
            }
            else
            {
                brandNewBuild = true;
            }

            AddNewIndexFiles(needToBuildIndex ?? allFiles, out var failedIndexFiles, brandNewBuild);
            GC.Collect(); // Run GC to save the memory

            IndexBuilder.Commit();

            if (failedIndexFiles.Count > 0 || failedUpdateOrDeleteFiles.Count > 0)
            {
                Log.LogError($"{IndexConfig.IndexName}: Initialize finished for {IndexConfig.MonitorFolder}, failed with these file(s): {string.Join(", ", failedIndexFiles.Select(u => u.FullName).Concat(failedUpdateOrDeleteFiles))}");
            }
            else
            {
                Log.LogInformation($"{IndexConfig.IndexName}: Initialize finished for {IndexConfig.MonitorFolder}");
            }
        }

        void AddNewIndexFiles(IEnumerable<FileInfo> needToBuildIndex, out List<FileInfo> failedIndexFiles, bool brandNewBuild)
        {
            failedIndexFiles = IndexBuilder.BuildIndexByBatch(needToBuildIndex, true, false, false, TokenSource.Token, brandNewBuild);

            if (failedIndexFiles.Count > 0)
            {
                Log.LogInformation($"{IndexConfig.IndexName}: Retry failed build indexes files, files count {failedIndexFiles.Count}");
                failedIndexFiles = IndexBuilder.BuildIndexByBatch(failedIndexFiles, true, false, false, TokenSource.Token, false);
            }
        }

        void OnChange(object sender, FileSystemEventArgs e)
        {
            EnqueueChangeSource(e.ChangeType, e.FullPath);
        }

        void EnqueueChangeSource(WatcherChangeTypes changeType, string fullPath)
        {
            var changeSource = new ChangedSource
            {
                ChangesType = changeType,
                FilePath = fullPath
            };

            if (!IsExcludedFromIndex(changeSource.FilePath))
            {
                ChangedSources.Enqueue(changeSource);
            }
        }

        void OnRename(object sender, RenamedEventArgs e)
        {
            var changeSource = new ChangedSource
            {
                ChangesType = e.ChangeType,
                FilePath = e.FullPath,
                OldPath = e.OldFullPath
            };

            ChangedSources.Enqueue(changeSource);
        }

        IndexConfig IndexConfig { get; }
        public CodeIndexConfiguration CodeIndexConfiguration { get; }
        public ILogger Log { get; }
        public IndexStatus Status { get; private set; }
        public CodeIndexBuilder IndexBuilder { get; private set; }
        public string Description { get; set; }
        public bool IsDisposing { get; private set; }
        FileSystemWatcher FilesWatcher { get; set; }
        public CancellationTokenSource TokenSource { get; }
        ConcurrentQueue<ChangedSource> ChangedSources { get; set; }
        ConcurrentQueue<PendingRetrySource> PendingRetryCodeSources { get; set; }
        string[] ExcludedExtensions { get; }
        string[] ExcludedPaths { get; }
        string[] IncludedExtensions { get; }

        public void Dispose()
        {
            if (!IsDisposing)
            {
                IsDisposing = true;
                Status = IndexStatus.Disposing;
                TokenSource.Cancel();
                TokenSource.Dispose();
                FilesWatcher?.Dispose();
                IndexBuilder?.Dispose();
                Status = IndexStatus.Disposed;
            }
        }

        bool IsExcludedFromIndex(string fullPath)
        {
            var excluded = ExcludedPaths.Any(u => fullPath.ToUpperInvariant().Contains(u))
                           || ExcludedExtensions.Any(u => fullPath.EndsWith(u, StringComparison.InvariantCultureIgnoreCase))
                           || fullPath.Contains(".") && IncludedExtensions.Length > 0 && !IncludedExtensions.Any(u => fullPath.EndsWith(u, StringComparison.InvariantCultureIgnoreCase));

            if (excluded)
            {
                Log.LogDebug($"{IndexConfig.IndexName}: {fullPath} is excluded from index");
            }

            return excluded;
        }

        protected virtual int FetchIntervalSeconds => 3;
    }
}
