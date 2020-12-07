using System;
using System.IO;
using CodeIndex.Common;
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
            IndexStatus = IndexStatus.Idle;
        }

        public void StartInitialize(bool forceRebuild)
        {
            try
            {
                IndexStatus = IndexStatus.Initializing;

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
                }
                else
                {
                    IndexStatus = IndexStatus.Error;
                    Description = "Monitor Folder Not Exist";
                }
            }
            catch (Exception ex)
            {
                IndexStatus = IndexStatus.Error;
                Description = ex.Message;
            }
        }

        public IndexConfig IndexConfig { get; }
        public CodeIndexConfiguration CodeIndexConfiguration { get; }
        public ILog Log { get; }
        public IndexStatus IndexStatus { get; private set; }
        public LucenePoolLight CodeIndexPool { get; private set; }
        public LucenePoolLight HintIndexPool { get; private set; }
        public CodeIndexBuilderLight CodeIndexBuilder { get; private set; }
        public string Description { get; set; }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
