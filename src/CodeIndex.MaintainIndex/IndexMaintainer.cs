using System;
using System.IO;
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
        }

        public void Start(bool forceRebuild)
        {
            if (Status != IndexStatus.Idle)
            {
                return;
            }

            try
            {
                Log.Info($"Start Initializing for {IndexConfig.IndexName}");
                Status = IndexStatus.Initializing;

                if (Directory.Exists(IndexConfig.MonitorFolder))
                {
                    var folders = IndexConfig.GetFolders(CodeIndexConfiguration.LuceneIndex);

                    CodeIndexPool = new LucenePoolLight(folders.CodeIndexFolder);
                    HintIndexPool = new LucenePoolLight(folders.HintIndexFolder);
                    CodeIndexBuilder = new CodeIndexBuilderLight(IndexConfig.IndexName, CodeIndexPool, HintIndexPool, Log);
                    CodeIndexBuilder.InitIndexFolderIfNeeded();

                    if (forceRebuild)
                    {
                        CodeIndexBuilder.DeleteAllIndex();
                    }

                    // TODO: Monitoring It

                    FilesWatcher = FilesWatcherHelper.StartWatch(IndexConfig.MonitorFolder, OnChange, OnRename);

                    // TODO: Do update, Do delete
                }
                else
                {
                    Status = IndexStatus.Error;
                    Description = "Monitor Folder Not Exist";
                    Log.Warn($"Initializing failed for {IndexConfig.IndexName}: {Description}");
                }
            }
            catch (Exception ex)
            {
                Status = IndexStatus.Error;
                Description = ex.Message;
                Log.Error($"Initializing failed for {IndexConfig.IndexName}: {ex}");
            }
        }

        void OnChange(object sender, FileSystemEventArgs e)
        {
            throw new NotImplementedException();
        }

        void OnRename(object sender, RenamedEventArgs e)
        {
            throw new NotImplementedException();
        }

        public IndexConfig IndexConfig { get; }
        public CodeIndexConfiguration CodeIndexConfiguration { get; }
        public ILog Log { get; }
        public IndexStatus Status { get; private set; }
        public LucenePoolLight CodeIndexPool { get; private set; }
        public LucenePoolLight HintIndexPool { get; private set; }
        public CodeIndexBuilderLight CodeIndexBuilder { get; private set; }
        public string Description { get; set; }
        public bool IsDisposing { get; private set; }
        FileSystemWatcher FilesWatcher { get; set; }

        public void Dispose()
        {
            if (!IsDisposing)
            {
                IsDisposing = true;
                FilesWatcher?.Dispose();
                CodeIndexPool?.Dispose();
                HintIndexPool?.Dispose();
            }
        }
    }
}
