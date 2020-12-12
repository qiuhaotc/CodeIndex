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

namespace CodeIndex.MaintainIndex
{
    public class IndexMaintainer : IDisposable
    {
        public IndexMaintainer(IndexConfig indexConfig, CodeIndexConfiguration codeIndexConfiguration, ILog log)
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
            IncludedExtensions = indexConfig.IncludedExtensionsArray?.Select(u => u.ToUpperInvariant()).ToArray() ?? Array.Empty<string>();
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
                Log.Info($"{IndexConfig.IndexName}: Start Initializing");
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
                    Log.Warn($"{IndexConfig.IndexName}: Initializing failed: {Description}");
                }
            }
            catch (Exception ex)
            {
                Status = IndexStatus.Error;
                Description = ex.Message;
                Log.Error($"{IndexConfig.IndexName}: Initializing failed: {ex}");
            }
        }

        public async Task MaintainIndexes()
        {
            if (Status != IndexStatus.Initialized)
            {
                return;
            }

            Log.Info($"{IndexConfig.IndexName}: Start Maintain Indexes");

            await MaintainIndexesCore();
        }

        async Task MaintainIndexesCore()
        {
            while (true)
            {
                TokenSource.Token.ThrowIfCancellationRequested();

                var fetchEndDate = DateTime.UtcNow.AddSeconds(-6);
                var notChangedDuring = fetchEndDate.AddSeconds(3);
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

                        IndexBuilderLight.Commit();
                    }
                }

                if (TokenSource.Token.IsCancellationRequested)
                {
                    throw new OperationCanceledException();
                }

                await Task.Delay(3000, TokenSource.Token);
            }
        }

        void ProcessingChanges(List<ChangedSource> orderedNeedProcessingChanges)
        {
            PreProcessingChanges(orderedNeedProcessingChanges);

            Log.Info($"{IndexConfig.IndexName}: Processing Changes start, changes count: {orderedNeedProcessingChanges.Count}");

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
                        Log.Warn($"{IndexConfig.IndexName}: Unknown changes type {changes}");
                        break;
                }
            }

            Log.Info($"{IndexConfig.IndexName}: Processing Changes finished");
        }

        void PreProcessingChanges(IList<ChangedSource> orderedNeedProcessingChanges)
        {
            Log.Debug($"{IndexConfig.IndexName}: Pre Processing Changes Start, changes count: {orderedNeedProcessingChanges.Count}");

            var needDeleted = new List<ChangedSource>();

            for (var i = 0; i < orderedNeedProcessingChanges.Count; i++)
            {
                var change = orderedNeedProcessingChanges[i];

                if (change.ChangesType == WatcherChangeTypes.Renamed)
                {
                    var templateRenameChange = orderedNeedProcessingChanges.Skip(i + 1).FirstOrDefault(u => u.ChangesType == WatcherChangeTypes.Renamed && u.FilePath == change.OldPath);

                    if (templateRenameChange != null)
                    {
                        change.ChangesType = WatcherChangeTypes.Changed;
                        change.FilePath = change.OldPath;
                        change.OldPath = null;
                        needDeleted.Add(templateRenameChange);

                        Log.Debug($"{IndexConfig.IndexName}: Template Change Found {templateRenameChange}, remove this and update {change} from Renamed to Changed");
                    }
                }
            }

            needDeleted.ForEach(u => orderedNeedProcessingChanges.Remove(u));
            Log.Debug($"{IndexConfig.IndexName}: Pre Processing Changes Finished");
        }

        void CreateIndex(ChangedSource changes)
        {
            if (IsFile(changes.FilePath))
            {
                IndexBuilderLight.CreateIndex(new FileInfo(changes.FilePath));
            }
        }

        void RenameIndex(ChangedSource changes)
        {
            if (IsExcludedFromIndex(changes.FilePath))
            {
                if (!IsExcludedFromIndex(changes.OldPath))
                {
                    IndexBuilderLight.DeleteIndex(changes.OldPath);
                }
            }
            else
            {
                if (IsFile(changes.FilePath))
                {
                    IndexBuilderLight.RenameFileIndex(changes.OldPath, changes.FilePath);
                }
                else if (IsDirectory(changes.FilePath))
                {
                    IndexBuilderLight.RenameFolderIndexes(changes.OldPath, changes.FilePath, TokenSource.Token);
                }
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
            IndexBuilderLight.DeleteIndex(changes.FilePath);
        }

        void UpdateIndex(ChangedSource changes)
        {
            if (IsFile(changes.FilePath))
            {
                IndexBuilderLight.UpdateIndex(new FileInfo(changes.FilePath), TokenSource.Token);
            }
        }

        void InitializeIndexCore(bool forceRebuild)
        {
            var folders = IndexConfig.GetFolders(CodeIndexConfiguration.LuceneIndex);

            IndexBuilderLight = new CodeIndexBuilderLight(
                IndexConfig.IndexName,
                new LucenePoolLight(folders.CodeIndexFolder),
                new LucenePoolLight(folders.HintIndexFolder),
                Log);

            IndexBuilderLight.InitIndexFolderIfNeeded();

            Status = IndexStatus.Initializing_ComponentInitializeFinished;

            ChangedSources = new ConcurrentQueue<ChangedSource>();
            PendingRetryCodeSources = new ConcurrentQueue<PendingRetrySource>();
            FilesWatcher = FilesWatcherHelper.StartWatch(IndexConfig.MonitorFolder, OnChange, OnRename);

            var allFiles = FilesFetcher.FetchAllFiles(IndexConfig.MonitorFolder, IndexConfig.ExcludedExtensionsArray, IndexConfig.ExcludedPathsArray, includedExtensions: IndexConfig.IncludedExtensionsArray, isInLinux: CodeIndexConfiguration.IsInLinux).ToList();
            Log.Info($"{IndexConfig.IndexName}: Fetching {allFiles.Count} files need to indexing");

            List<FileInfo> needToBuildIndex = null;
            var failedUpdateOrDeleteFiles = new List<string>();

            if (CodeIndexBuilder.IndexExists(IndexBuilderLight.CodeIndexPool.LuceneIndex))
            {
                if (forceRebuild)
                {
                    Log.Info($"{IndexConfig.IndexName}: Force rebuild all indexes");
                    IndexBuilderLight.DeleteAllIndex();
                }
                else
                {
                    Log.Info($"{IndexConfig.IndexName}: Compare index difference");

                    var allCodeSource = IndexBuilderLight.GetAllIndexedCodeSource();

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
                                Log.Info($"{IndexConfig.IndexName}: File {fileInfo.FullName} modified");
                                if (!IndexBuilderLight.UpdateIndex(fileInfo, TokenSource.Token))
                                {
                                    failedUpdateOrDeleteFiles.Add(codeSource.FilePath);
                                }
                            }

                            allFilesDictionary.Remove(codeSource.FilePath);
                        }
                        else
                        {
                            Log.Info($"{IndexConfig.IndexName}: File {codeSource.FilePath} deleted");
                            if (!IndexBuilderLight.DeleteIndex(codeSource.FilePath))
                            {
                                failedUpdateOrDeleteFiles.Add(codeSource.FilePath);
                            }
                        }
                    }

                    foreach (var needToCreateFiles in allFilesDictionary)
                    {
                        Log.Info($"{IndexConfig.IndexName}: Found new file {needToCreateFiles.Value.FullName}");
                        needToBuildIndex.Add(needToCreateFiles.Value);
                    }
                }
            }

            AddNewIndexFiles(needToBuildIndex ?? allFiles, out var failedIndexFiles);

            IndexBuilderLight.Commit();

            if (failedIndexFiles.Count > 0 || failedUpdateOrDeleteFiles.Count > 0)
            {
                Log.Warn($"{IndexConfig.IndexName}: Initialize finished for {IndexConfig.MonitorFolder}, failed with these file(s): {string.Join(", ", failedIndexFiles.Select(u => u.FullName).Concat(failedUpdateOrDeleteFiles))}");
            }
            else
            {
                Log.Info($"{IndexConfig.IndexName}: Initialize finished for {IndexConfig.MonitorFolder}");
            }
        }

        void AddNewIndexFiles(IEnumerable<FileInfo> needToBuildIndex, out ConcurrentBag<FileInfo> failedIndexFiles)
        {
            failedIndexFiles = IndexBuilderLight.BuildIndexByBatch(needToBuildIndex, true, false, false, TokenSource.Token);

            if (failedIndexFiles.Count > 0)
            {
                Log.Info($"{IndexConfig.IndexName}: Retry failed build indexes files, files count {failedIndexFiles.Count}");
                failedIndexFiles = IndexBuilderLight.BuildIndexByBatch(failedIndexFiles, true, false, false, TokenSource.Token);
            }
        }

        void OnChange(object sender, FileSystemEventArgs e)
        {
            var changeSource = new ChangedSource
            {
                ChangesType = e.ChangeType,
                FilePath = e.FullPath
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
        public ILog Log { get; }
        public IndexStatus Status { get; private set; }
        public CodeIndexBuilderLight IndexBuilderLight { get; private set; }
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
                IndexBuilderLight?.Dispose();
                Status = IndexStatus.Disposed;
            }
        }

        bool IsExcludedFromIndex(string fullPath)
        {
            var excluded = ExcludedPaths.Any(u => fullPath.ToUpperInvariant().Contains(u))
                           || ExcludedExtensions.Any(u => fullPath.EndsWith(u, StringComparison.InvariantCultureIgnoreCase))
                           || IncludedExtensions.Length > 0 && !IncludedExtensions.Any(u => fullPath.EndsWith(u, StringComparison.InvariantCultureIgnoreCase));

            if (excluded)
            {
                Log.Debug($"{IndexConfig.IndexName}: {fullPath} is excluded from index");
            }

            return excluded;
        }
    }
}
