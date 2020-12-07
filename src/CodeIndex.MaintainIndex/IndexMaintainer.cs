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
        }

        public async Task InitializeIndex(bool forceRebuild, CancellationToken cancellationToken)
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
                        InitializeIndexCore(forceRebuild, cancellationToken);
                    }, cancellationToken);

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

        public async Task MaintainIndexes(CancellationToken cancellationToken)
        {
            if (Status != IndexStatus.Initialized)
            {
                return;
            }

            await Task.Run(() =>
            {
                MaintainIndexesCore(cancellationToken);
            }, cancellationToken);
        }

        void MaintainIndexesCore(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        void InitializeIndexCore(bool forceRebuild, CancellationToken cancellationToken)
        {
            var folders = IndexConfig.GetFolders(CodeIndexConfiguration.LuceneIndex);

            IndexBuilderLight = new CodeIndexBuilderLight(
                IndexConfig.IndexName,
                new LucenePoolLight(folders.CodeIndexFolder),
                new LucenePoolLight(folders.HintIndexFolder),
                Log);

            IndexBuilderLight.InitIndexFolderIfNeeded();

            ChangedSources = new ConcurrentBag<ChangedSource>();
            PendingRetryCodeSources = new ConcurrentQueue<PendingRetrySource>();
            FilesWatcher = FilesWatcherHelper.StartWatch(IndexConfig.MonitorFolder, OnChange, OnRename);

            var allFiles = FilesFetcher.FetchAllFiles(IndexConfig.MonitorFolder, IndexConfig.ExcludedExtensionsArray, IndexConfig.ExcludedPathsArray, includedExtensions: IndexConfig.IncludedExtensionsArray, isInLinux: CodeIndexConfiguration.IsInLinux).ToList();
            Log.Info($"{IndexConfig.IndexName}: Fetching {allFiles.Count} files need to indexing");

            List<FileInfo> needToBuildIndex = null;
            List<string> failedUpdateOrDeleteFiles = new List<string>();

            if (CodeIndexBuilder.IndexExists(IndexConfig.MonitorFolder))
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
                    needToBuildIndex = new List<FileInfo>();
                    var allFilesDictionary = allFiles.ToDictionary(u => u.FullName);

                    foreach (var codeSource in allCodeSource)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        if (allFilesDictionary.TryGetValue(codeSource.FilePath, out var fileInfo))
                        {
                            if (fileInfo.LastWriteTimeUtc != codeSource.LastWriteTimeUtc)
                            {
                                Log.Info($"{IndexConfig.IndexName}: File {fileInfo.FullName} modified");
                                if (!IndexBuilderLight.UpdateIndex(fileInfo, cancellationToken))
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

            AddNewIndexFiles(needToBuildIndex ?? allFiles, out var failedIndexFiles, cancellationToken);

            IndexBuilderLight.Commit();

            if (failedIndexFiles.Count > 0)
            {
                Log.Warn($"{IndexConfig.IndexName}: Initialize finished for {IndexConfig.MonitorFolder}, failed with these file(s): {string.Join(", ", failedIndexFiles.Select(u => u.FullName).Concat(failedUpdateOrDeleteFiles))}");
            }
            else
            {
                Log.Info($"{IndexConfig.IndexName}: Initialize finished for {IndexConfig.MonitorFolder}");
            }
        }

        void AddNewIndexFiles(List<FileInfo> needToBuildIndex, out List<FileInfo> failedIndexFiles, CancellationToken cancellationToken)
        {
            IndexBuilderLight.BuildIndexByBatch(needToBuildIndex, out failedIndexFiles, true, false, false, cancellationToken);

            if (failedIndexFiles.Count > 0)
            {
                Log.Info($"{IndexConfig.IndexName}: Retry failed build indexes files, files count {failedIndexFiles.Count}");
                IndexBuilderLight.BuildIndexByBatch(failedIndexFiles, out failedIndexFiles, true, false, false, cancellationToken);
            }
        }

        void OnChange(object sender, FileSystemEventArgs e)
        {
            ChangedSources.Add(new ChangedSource
            {
                ChangesType = e.ChangeType,
                FilePath = e.FullPath
            });
        }

        void OnRename(object sender, RenamedEventArgs e)
        {
            ChangedSources.Add(new ChangedSource
            {
                ChangesType = e.ChangeType,
                FilePath = e.FullPath,
                OldPath = e.OldFullPath
            });
        }

        public IndexConfig IndexConfig { get; }
        public CodeIndexConfiguration CodeIndexConfiguration { get; }
        public ILog Log { get; }
        public IndexStatus Status { get; private set; }
        public CodeIndexBuilderLight IndexBuilderLight { get; private set; }
        public string Description { get; set; }
        public bool IsDisposing { get; private set; }
        FileSystemWatcher FilesWatcher { get; set; }
        ConcurrentBag<ChangedSource> ChangedSources { get; set; }
        ConcurrentQueue<PendingRetrySource> PendingRetryCodeSources { get; set; }
        string[] ExcludedExtensions { get; }
        string[] ExcludedPaths { get; }
        string[] IncludedExtensions { get; }

        public void Dispose()
        {
            if (!IsDisposing)
            {
                IsDisposing = true;
                FilesWatcher?.Dispose();
                IndexBuilderLight?.Dispose();
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
