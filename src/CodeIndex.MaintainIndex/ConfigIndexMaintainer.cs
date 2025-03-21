﻿using System;
using System.Collections.Generic;
using System.IO;
using CodeIndex.Common;
using CodeIndex.IndexBuilder;
using Microsoft.Extensions.Logging;

namespace CodeIndex.MaintainIndex
{
    public class ConfigIndexMaintainer : IDisposable
    {
        public ConfigIndexMaintainer(CodeIndexConfiguration codeIndexConfiguration, ILogger log)
        {
            codeIndexConfiguration.RequireNotNull(nameof(codeIndexConfiguration));
            log.RequireNotNull(nameof(log));

            CodeIndexConfiguration = codeIndexConfiguration;
            Log = log;

            var folder = Path.Combine(codeIndexConfiguration.LuceneIndex, CodeIndexConfiguration.ConfigurationIndexFolder);

            if (!Directory.Exists(folder))
            {
                Log.LogInformation($"Create Configuraion index folder {folder}");

                try
                {
                    Directory.CreateDirectory(folder);
                }
                catch (Exception ex)
                {
                    var newFolder = Path.Combine(AppContext.BaseDirectory, CodeIndexConfiguration.ConfigurationIndexFolder);

                    Log.LogWarning(ex, $"Create Configuraion index folder {folder} failed, fallback to create index folder under {newFolder}");

                    try
                    {
                        Directory.CreateDirectory(newFolder);
                        CodeIndexConfiguration.LuceneIndex = AppContext.BaseDirectory;
                        folder = newFolder;
                    }
                    catch (Exception ex2)
                    {
                        Log.LogError(ex2, $"Create Configuraion index folder {folder} failed");
                    }
                }
            }

            ConfigIndexBuilder = new ConfigIndexBuilder(folder);
        }

        public IEnumerable<IndexConfig> GetConfigs()
        {
            return ConfigIndexBuilder.GetConfigs();
        }

        public void AddIndexConfig(IndexConfig indexConfig)
        {
            ConfigIndexBuilder.AddIndexConfig(indexConfig);
        }

        public void DeleteIndexConfig(Guid pk)
        {
            ConfigIndexBuilder.DeleteIndexConfig(pk);
        }

        public void EditIndexConfig(IndexConfig indexConfig)
        {
            ConfigIndexBuilder.EditIndexConfig(indexConfig);
        }

        public bool IsDisposing { get; private set; }
        public CodeIndexConfiguration CodeIndexConfiguration { get; }
        public ILogger Log { get; }
        ConfigIndexBuilder ConfigIndexBuilder { get; }

        public void Dispose()
        {
            if (!IsDisposing)
            {
                IsDisposing = true;
                ConfigIndexBuilder.Dispose();
            }
        }
    }
}
