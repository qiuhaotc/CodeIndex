using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using CodeIndex.Common;

namespace CodeIndex.MaintainIndex
{
    public class IndexManagement : IDisposable
    {
        ConcurrentDictionary<string, IndexMaintainerWrapper> MaintainerPool { get; set; }
        CodeIndexConfiguration CodeIndexConfiguration { get; }
        ILog Log { get; }
        ConfigIndexMaintainer ConfigMaintainer { get; }
        public bool IsDisposing { get; private set; }

        object syncLock = new object();

        public IndexManagement(CodeIndexConfiguration codeIndexConfiguration, ILog log)
        {
            codeIndexConfiguration.RequireNotNull(nameof(codeIndexConfiguration));
            log.RequireNotNull(nameof(log));

            MaintainerPool = new ConcurrentDictionary<string, IndexMaintainerWrapper>();
            CodeIndexConfiguration = codeIndexConfiguration;
            Log = log;
            ConfigMaintainer = new ConfigIndexMaintainer(codeIndexConfiguration, log);

            InitializeMaintainerPool();
        }

        void InitializeMaintainerPool()
        {
            var allConfigs = ConfigMaintainer.GetConfigs();

            foreach (var config in allConfigs)
            {
                MaintainerPool.TryAdd(config.IndexName, new IndexMaintainerWrapper(config, CodeIndexConfiguration, Log));
            }
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

        public FetchResult<string[]> GetIndexNameList()
        {
            if (IsDisposing)
            {
                return ManagementIsDisposing<string[]>();
            }

            return new FetchResult<string[]>
            {
                Result = MaintainerPool.Select(u => u.Value.IndexConfig.IndexName).ToArray(),
                Status = new Status
                {
                    Success = true
                }
            };
        }

        public FetchResult<bool> AddIndex(IndexConfig indexConfig)
        {
            if (IsDisposing)
            {
                return ManagementIsDisposing<bool>();
            }

            lock (syncLock)
            {
                if (ValidToAdd(indexConfig, out var message))
                {
                    ConfigMaintainer.AddIndexConfig(indexConfig);
                    MaintainerPool.TryAdd(indexConfig.IndexName, new IndexMaintainerWrapper(indexConfig, CodeIndexConfiguration, Log));

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
                if (ValidToEdit(indexConfig, out var message, out var needDisposeAndRemovedWrapper) && needDisposeAndRemovedWrapper != null)
                {
                    ConfigMaintainer.EditIndexConfig(indexConfig);
                    needDisposeAndRemovedWrapper.Dispose();
                    MaintainerPool.TryUpdate(indexConfig.IndexName, new IndexMaintainerWrapper(indexConfig, CodeIndexConfiguration, Log), needDisposeAndRemovedWrapper);

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

        public FetchResult<bool> DeleteIndex(string indexName)
        {
            if (IsDisposing)
            {
                return ManagementIsDisposing<bool>();
            }

            lock (syncLock)
            {
                if (ValidToDelete(indexName, out var message, out var needDisposeAndRemovedWrapper) && needDisposeAndRemovedWrapper != null)
                {
                    needDisposeAndRemovedWrapper.Dispose();
                    DeleteIndexFolder(needDisposeAndRemovedWrapper.IndexConfig.MonitorFolder);

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

        public FetchResult<bool> StopIndex(string indexName)
        {
            if (IsDisposing)
            {
                return ManagementIsDisposing<bool>();
            }

            lock (syncLock)
            {
                if (ValidToStop(indexName, out var message, out var needDisposeWrapper) && needDisposeWrapper != null)
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

        public FetchResult<IndexMaintainerWrapper> GetIndexMaintainerWrapper(string indexName)
        {
            if (IsDisposing)
            {
                return ManagementIsDisposing<IndexMaintainerWrapper>();
            }

            if (MaintainerPool.TryGetValue(indexName, out var wrapper))
            {
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

        void DeleteIndexFolder(string monitorFolder)
        {
            Log.Info($"Delete all files under {monitorFolder}");
            Directory.Delete(monitorFolder, true);
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
                validationMessage += "Index Name Is Empty" + Environment.NewLine;
            }
            else if (MaintainerPool.ContainsKey(indexConfig.IndexName))
            {
                validationMessage += "Duplicate Index Name" + Environment.NewLine;
            }

            if (string.IsNullOrWhiteSpace(indexConfig.MonitorFolder))
            {
                validationMessage += "Monitor Folder Is Empty" + Environment.NewLine;
            }
            else if (MaintainerPool.Any(u => u.Value.IndexConfig.MonitorFolder.Equals(indexConfig.MonitorFolder, StringComparison.InvariantCultureIgnoreCase)))
            {
                validationMessage += "Duplicate Monitor Folder" + Environment.NewLine;
            }

            return string.IsNullOrEmpty(validationMessage);
        }

        bool ValidToEdit(IndexConfig indexConfig, out string validationMessage, out IndexMaintainerWrapper needDisposeAndRemovedWrapper)
        {
            validationMessage = string.Empty;
            needDisposeAndRemovedWrapper = null;

            if (string.IsNullOrWhiteSpace(indexConfig.IndexName))
            {
                validationMessage += "Index Name Is Empty" + Environment.NewLine;
            }
            else if (!MaintainerPool.TryGetValue(indexConfig.IndexName, out needDisposeAndRemovedWrapper))
            {
                validationMessage += "Index Not Exist" + Environment.NewLine;
            }
            else if (needDisposeAndRemovedWrapper.Status != IndexStatus.Idle && needDisposeAndRemovedWrapper.Status != IndexStatus.Disposed)
            {
                validationMessage += "Only Idle or Disposed Index Can Be Edit" + Environment.NewLine;
            }

            if (string.IsNullOrWhiteSpace(indexConfig.MonitorFolder))
            {
                validationMessage += "Monitor Folder Is Empty" + Environment.NewLine;
            }

            return string.IsNullOrEmpty(validationMessage);
        }

        bool ValidToDelete(string indexName, out string validationMessage, out IndexMaintainerWrapper needDisposeAndRemovedWrapper)
        {
            validationMessage = string.Empty;
            needDisposeAndRemovedWrapper = null;

            if (string.IsNullOrWhiteSpace(indexName))
            {
                validationMessage += "Index Name Is Empty" + Environment.NewLine;
            }
            else if (!MaintainerPool.TryGetValue(indexName, out needDisposeAndRemovedWrapper))
            {
                validationMessage += "Index Not Exist" + Environment.NewLine;
            }
            else if (needDisposeAndRemovedWrapper.Status == IndexStatus.Disposing)
            {
                validationMessage += "Index Under Disposing Status" + Environment.NewLine;
            }

            return string.IsNullOrEmpty(validationMessage);
        }

        bool ValidToStop(string indexName, out string validationMessage, out IndexMaintainerWrapper needDisposeWrapper)
        {
            validationMessage = string.Empty;
            needDisposeWrapper = null;

            if (string.IsNullOrWhiteSpace(indexName))
            {
                validationMessage += "Index Name Is Empty" + Environment.NewLine;
            }
            else if (!MaintainerPool.TryGetValue(indexName, out needDisposeWrapper))
            {
                validationMessage += "Index Not Exist" + Environment.NewLine;
            }
            else if (needDisposeWrapper.Status == IndexStatus.Disposed || needDisposeWrapper.Status == IndexStatus.Disposing)
            {
                validationMessage += "Index Under Disposed/Disposing Status" + Environment.NewLine;
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
                        Log.Error($"Index management failed to dispose {item.Value.IndexConfig.IndexName}, error: {ex}");
                    }
                }

                MaintainerPool.Clear();
                ConfigMaintainer.Dispose();
            }
        }
    }
}
