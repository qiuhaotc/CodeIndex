using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading;
using CodeIndex.Common;
using Microsoft.Extensions.Logging;

namespace CodeIndex.MaintainIndex
{
    public class IndexManagement : IDisposable
    {
        ConcurrentDictionary<Guid, IndexMaintainerWrapper> MaintainerPool { get; set; }
        CodeIndexConfiguration CodeIndexConfiguration { get; }
        ILogger<IndexManagement> Log { get; }
        ConfigIndexMaintainer ConfigMaintainer { get; }
        public bool IsDisposing { get; private set; }

        object syncLock = new object();

        public IndexManagement(CodeIndexConfiguration codeIndexConfiguration, ILogger<IndexManagement> log)
        {
            codeIndexConfiguration.RequireNotNull(nameof(codeIndexConfiguration));
            log.RequireNotNull(nameof(log));

            MaintainerPool = new ConcurrentDictionary<Guid, IndexMaintainerWrapper>();
            CodeIndexConfiguration = codeIndexConfiguration;
            Log = log;
            ConfigMaintainer = new ConfigIndexMaintainer(codeIndexConfiguration, log);

            InitializeMaintainerPool();
        }

        void InitializeMaintainerPool()
        {
            var allConfigs = ConfigMaintainer.GetConfigs();

            Log.LogInformation("Initialize Maintainer Pool Start");

            foreach (var config in allConfigs)
            {
                MaintainerPool.TryAdd(config.Pk, new IndexMaintainerWrapper(config, CodeIndexConfiguration, Log));
            }

            Log.LogInformation("Initialize Maintainer Pool Finished");
        }

        public FetchResult<IndexStatusInfo[]> GetIndexList()
        {
            if (IsDisposing)
            {
                return ManagementIsDisposing<IndexStatusInfo[]>();
            }

            return new FetchResult<IndexStatusInfo[]>
            {
                Result = MaintainerPool.Select(u => new IndexStatusInfo(u.Value.Status, u.Value.IndexConfig)).ToArray(),
                Status = new Status
                {
                    Success = true
                }
            };
        }

        readonly IndexStatus[] validStatusForSearching = { IndexStatus.Idle, IndexStatus.Initializing, IndexStatus.Initialized, IndexStatus.Initializing_ComponentInitializeFinished, IndexStatus.Monitoring };

        public static bool ValidToStart(IndexStatus status) => status == IndexStatus.Disposed || status == IndexStatus.Idle;

        public FetchResult<IndexConfigForView[]> GetIndexViewList()
        {
            if (IsDisposing)
            {
                return ManagementIsDisposing<IndexConfigForView[]>();
            }

            return new FetchResult<IndexConfigForView[]>
            {
                Result = MaintainerPool.Where(u => validStatusForSearching.Contains(u.Value.Status)).Select(u => IndexConfigForView.GetIndexConfigForView(u.Value.IndexConfig)).ToArray(),
                Status = new Status
                {
                    Success = true
                }
            };
        }

        public IndexConfigForView GetIndexView(Guid pk)
        {
            if (IsDisposing)
            {
                return new IndexConfigForView();
            }

            IndexConfigForView indexConfigForView;

            if (MaintainerPool.TryGetValue(pk, out var wrapper))
            {
                indexConfigForView = IndexConfigForView.GetIndexConfigForView(wrapper.IndexConfig);
            }
            else
            {
                indexConfigForView = new IndexConfigForView();
            }

            return indexConfigForView;
        }

        public FetchResult<bool> AddIndex(IndexConfig indexConfig)
        {
            if (IsDisposing)
            {
                return ManagementIsDisposing<bool>();
            }

            lock (syncLock)
            {
                indexConfig.TrimValues();

                if (ValidToAdd(indexConfig, out var message))
                {
                    ConfigMaintainer.AddIndexConfig(indexConfig);
                    MaintainerPool.TryAdd(indexConfig.Pk, new IndexMaintainerWrapper(indexConfig, CodeIndexConfiguration, Log));

                    Log.LogInformation($"Add Index Config {indexConfig} Successful");

                    return new FetchResult<bool>
                    {
                        Result = true,
                        Status = new Status
                        {
                            Success = true
                        }
                    };
                }
                else
                {
                    return new FetchResult<bool>
                    {
                        Result = false,
                        Status = new Status
                        {
                            StatusDesc = message,
                            Success = false
                        }
                    };
                }
            }
        }

        public FetchResult<bool> EditIndex(IndexConfig indexConfig)
        {
            if (IsDisposing)
            {
                return ManagementIsDisposing<bool>();
            }

            lock (syncLock)
            {
                indexConfig.TrimValues();

                if (ValidToEdit(indexConfig, out var message, out var needDisposeAndRemovedWrapper) && needDisposeAndRemovedWrapper != null)
                {
                    ConfigMaintainer.EditIndexConfig(indexConfig);
                    needDisposeAndRemovedWrapper.Dispose();
                    MaintainerPool.TryUpdate(indexConfig.Pk, new IndexMaintainerWrapper(indexConfig, CodeIndexConfiguration, Log), needDisposeAndRemovedWrapper);

                    Log.LogInformation($"Edit Index Config {indexConfig} Successful");

                    return new FetchResult<bool>
                    {
                        Result = true,
                        Status = new Status
                        {
                            Success = true
                        }
                    };
                }
                else
                {
                    return new FetchResult<bool>
                    {
                        Result = false,
                        Status = new Status
                        {
                            StatusDesc = message,
                            Success = false
                        }
                    };
                }
            }
        }

        public FetchResult<bool> DeleteIndex(Guid pk)
        {
            if (IsDisposing)
            {
                return ManagementIsDisposing<bool>();
            }

            lock (syncLock)
            {
                if (ValidToDelete(pk, out var message, out var needDisposeAndRemovedWrapper) && needDisposeAndRemovedWrapper != null)
                {
                    needDisposeAndRemovedWrapper.Dispose();
                    DeleteIndexFolderSafe(needDisposeAndRemovedWrapper.IndexConfig.GetRootFolder(CodeIndexConfiguration.LuceneIndex));
                    if (MaintainerPool.TryRemove(pk, out _))
                    {
                        ConfigMaintainer.DeleteIndexConfig(pk);
                    }

                    Log.LogInformation($"Delete Index Config {needDisposeAndRemovedWrapper.IndexConfig} Successful");

                    return new FetchResult<bool>
                    {
                        Result = true,
                        Status = new Status
                        {
                            Success = true
                        }
                    };
                }
                else
                {
                    return new FetchResult<bool>
                    {
                        Result = false,
                        Status = new Status
                        {
                            StatusDesc = message,
                            Success = false
                        }
                    };
                }
            }
        }

        public FetchResult<bool> StartIndex(Guid pk)
        {
            if (IsDisposing)
            {
                return ManagementIsDisposing<bool>();
            }

            lock (syncLock)
            {
                if (ValidToStart(pk, out var message, out var needDisposeWrapper) && needDisposeWrapper != null)
                {
                    if (needDisposeWrapper.Status != IndexStatus.Idle)
                    {
                        needDisposeWrapper.Dispose();
                        MaintainerPool.TryUpdate(pk, new IndexMaintainerWrapper(needDisposeWrapper.IndexConfig, CodeIndexConfiguration, Log), needDisposeWrapper);
                    }

                    var result = GetIndexMaintainerWrapperAndInitializeIfNeeded(pk);

                    if (result.Status.Success)
                    {
                        Log.LogInformation($"Start Index Config {needDisposeWrapper.IndexConfig} Successful");
                    }
                    else
                    {
                        Log.LogInformation($"Start Index Config {needDisposeWrapper.IndexConfig} Failed: {result.Status.StatusDesc}");
                    }

                    return new FetchResult<bool>
                    {
                        Result = result.Status.Success,
                        Status = new Status
                        {
                            Success = result.Status.Success,
                            StatusDesc = result.Status.StatusDesc
                        }
                    };
                }
                else
                {
                    return new FetchResult<bool>
                    {
                        Result = false,
                        Status = new Status
                        {
                            StatusDesc = message,
                            Success = false
                        }
                    };
                }
            }
        }

        public FetchResult<bool> StopIndex(Guid pk)
        {
            if (IsDisposing)
            {
                return ManagementIsDisposing<bool>();
            }

            lock (syncLock)
            {
                if (ValidToStop(pk, out var message, out var needDisposeWrapper) && needDisposeWrapper != null)
                {
                    needDisposeWrapper.Dispose();

                    return new FetchResult<bool>
                    {
                        Result = true,
                        Status = new Status
                        {
                            Success = true
                        }
                    };
                }
                else
                {
                    return new FetchResult<bool>
                    {
                        Result = false,
                        Status = new Status
                        {
                            StatusDesc = message,
                            Success = false
                        }
                    };
                }
            }
        }

        object syncLockForInitializeMaintainer = new object();

        public FetchResult<IndexMaintainerWrapper> GetIndexMaintainerWrapperAndInitializeIfNeeded(Guid pk)
        {
            if (IsDisposing)
            {
                return ManagementIsDisposing<IndexMaintainerWrapper>();
            }

            if (MaintainerPool.TryGetValue(pk, out var wrapper))
            {
                // Make sure InitializeIndex and MaintainIndexes only call once, make sure the index pool initialized and able to searching
                if (wrapper.Maintainer.Status == IndexStatus.Idle || wrapper.Maintainer.Status == IndexStatus.Initializing)
                {
                    lock (syncLockForInitializeMaintainer)
                    {
                        if (wrapper.Maintainer.Status == IndexStatus.Idle)
                        {
                            Log.LogInformation($"Start initializing and monitoring for index {wrapper.IndexConfig.IndexName}");
                            wrapper.Maintainer.InitializeIndex(false).ContinueWith(u => wrapper.Maintainer.MaintainIndexes());
                        }

                        if (wrapper.Maintainer.Status == IndexStatus.Initializing)
                        {
                            while (wrapper.Maintainer.Status == IndexStatus.Initializing) // Wait Maintainer able to screening
                            {
                                Thread.Sleep(100);
                            }
                        }
                    }
                }

                return new FetchResult<IndexMaintainerWrapper>
                {
                    Result = wrapper,
                    Status = new Status
                    {
                        Success = true
                    }
                };
            }

            return new FetchResult<IndexMaintainerWrapper>
            {
                Status = new Status
                {
                    Success = false,
                    StatusDesc = "Index Not Exist"
                }
            };
        }

        void DeleteIndexFolderSafe(string monitorFolder)
        {
            Log.LogInformation($"Delete all files under {monitorFolder}");

            try
            {
                if (Directory.Exists(monitorFolder))
                {
                    Directory.Delete(monitorFolder, true);
                }
            }
            catch (Exception ex)
            {
                Log.LogInformation($"Failed to delete all files under {monitorFolder}, exception: {ex}");
            }
        }

        FetchResult<T> ManagementIsDisposing<T>()
        {
            return new FetchResult<T>
            {
                Result = default,
                Status = new Status
                {
                    StatusDesc = "Index Management Is Disposing",
                    Success = false
                }
            };
        }

        bool ValidToAdd(IndexConfig indexConfig, out string validationMessage)
        {
            validationMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(indexConfig.IndexName))
            {
                validationMessage += "Index Name Is Empty;" + Environment.NewLine;
            }
            else if (indexConfig.IndexName.Length > 100)
            {
                validationMessage += $"Index Name Too Long;" + Environment.NewLine;
            }
            else if (MaintainerPool.Any(u => u.Value.IndexConfig.IndexName == indexConfig.IndexName))
            {
                validationMessage += $"Duplicate Index Name;" + Environment.NewLine;
            }

            if (MaintainerPool.ContainsKey(indexConfig.Pk))
            {
                validationMessage += "Duplicate Index Pk;" + Environment.NewLine;
            }

            if (string.IsNullOrWhiteSpace(indexConfig.MonitorFolder))
            {
                validationMessage += "Monitor Folder Is Empty;" + Environment.NewLine;
            }
            else if (MaintainerPool.Any(u => u.Value.IndexConfig.MonitorFolder.Equals(indexConfig.MonitorFolder, StringComparison.InvariantCultureIgnoreCase)))
            {
                validationMessage += "Duplicate Monitor Folder;" + Environment.NewLine;
            }
            else if (!Directory.Exists(indexConfig.MonitorFolder))
            {
                validationMessage += $"Monitor Folder Not Exist; Base Dir: {AppContext.BaseDirectory}" + Environment.NewLine;
            }

            return string.IsNullOrEmpty(validationMessage);
        }

        public static bool ValidToEdit(IndexStatus status) => status == IndexStatus.Idle || status == IndexStatus.Disposed;

        bool ValidToEdit(IndexConfig indexConfig, out string validationMessage, out IndexMaintainerWrapper needDisposeAndRemovedWrapper)
        {
            validationMessage = string.Empty;
            needDisposeAndRemovedWrapper = null;

            if (string.IsNullOrWhiteSpace(indexConfig.IndexName))
            {
                validationMessage += "Index Name Is Empty;" + Environment.NewLine;
            }
            else if (indexConfig.IndexName.Length > 100)
            {
                validationMessage += "Index Name Too Long;" + Environment.NewLine;
            }
            else if (!MaintainerPool.TryGetValue(indexConfig.Pk, out needDisposeAndRemovedWrapper))
            {
                validationMessage += "Index Not Exist;" + Environment.NewLine;
            }
            else if (!ValidToEdit(needDisposeAndRemovedWrapper.Status))
            {
                validationMessage += "Only Idle or Disposed Index Can Be Edit;" + Environment.NewLine;
            }
            else if (MaintainerPool.Any(u => u.Key != indexConfig.Pk && u.Value.IndexConfig.IndexName == indexConfig.IndexName))
            {
                validationMessage += "Duplicate Index Name;" + Environment.NewLine;
            }

            if (string.IsNullOrWhiteSpace(indexConfig.MonitorFolder))
            {
                validationMessage += "Monitor Folder Is Empty;" + Environment.NewLine;
            }
            else if (!Directory.Exists(indexConfig.MonitorFolder))
            {
                validationMessage += "Monitor Folder Not Exist;" + Environment.NewLine;
            }

            return string.IsNullOrEmpty(validationMessage);
        }

        bool ValidToDelete(Guid pk, out string validationMessage, out IndexMaintainerWrapper needDisposeAndRemovedWrapper)
        {
            validationMessage = string.Empty;

            if (!MaintainerPool.TryGetValue(pk, out needDisposeAndRemovedWrapper))
            {
                validationMessage += "Index Not Exist;" + Environment.NewLine;
            }
            else if (needDisposeAndRemovedWrapper.Status == IndexStatus.Disposing)
            {
                validationMessage += "Index Under Disposing Status;" + Environment.NewLine;
            }

            return string.IsNullOrEmpty(validationMessage);
        }

        bool ValidToStart(Guid pk, out string validationMessage, out IndexMaintainerWrapper needDisposedWrapper)
        {
            validationMessage = string.Empty;

            if (!MaintainerPool.TryGetValue(pk, out needDisposedWrapper))
            {
                validationMessage += "Index Not Exist;" + Environment.NewLine;
            }
            else if (!ValidToStart(needDisposedWrapper.Status))
            {
                validationMessage += "Index Not Valid To Start;" + Environment.NewLine;
            }

            return string.IsNullOrEmpty(validationMessage);
        }

        bool ValidToStop(Guid pk, out string validationMessage, out IndexMaintainerWrapper needDisposeWrapper)
        {
            validationMessage = string.Empty;

            if (!MaintainerPool.TryGetValue(pk, out needDisposeWrapper))
            {
                validationMessage += "Index Not Exist;" + Environment.NewLine;
            }
            else if (needDisposeWrapper.Status == IndexStatus.Disposed || needDisposeWrapper.Status == IndexStatus.Disposing)
            {
                validationMessage += "Index Under Disposed/Disposing Status;" + Environment.NewLine;
            }

            return string.IsNullOrEmpty(validationMessage);
        }

        public void Dispose()
        {
            if (!IsDisposing)
            {
                IsDisposing = true;
                foreach (var item in MaintainerPool)
                {
                    try
                    {
                        item.Value.Dispose();
                    }
                    catch (Exception ex)
                    {
                        Log.LogError($"Index management failed to dispose {item.Value.IndexConfig.IndexName}, error: {ex}");
                    }
                }

                MaintainerPool.Clear();
                ConfigMaintainer.Dispose();
            }
        }
    }
}
