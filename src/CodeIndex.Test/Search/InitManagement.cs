using System;
using System.IO;
using System.Threading;
using CodeIndex.Common;
using CodeIndex.MaintainIndex;
using CodeIndex.Search;
using Microsoft.Extensions.Logging;

namespace CodeIndex.Test
{
    public class InitManagement : IDisposable
    {
        readonly IndexManagement management;
        readonly IndexConfig indexConfig;
        readonly ILogger<IndexManagement> log1 = new DummyLog<IndexManagement>();
        readonly ILogger<CodeIndexSearcher> log2 = new DummyLog<CodeIndexSearcher>();

        public InitManagement(string monitorFolder, CodeIndexConfiguration codeIndexConfiguration, bool initFiles = false, int maxContentHighlightLength = Constants.DefaultMaxContentHighlightLength)
        {
            indexConfig = new IndexConfig
            {
                IndexName = "ABC",
                MonitorFolder = monitorFolder,
                MaxContentHighlightLength = maxContentHighlightLength
            };

            if (initFiles)
            {
                var fileName1 = Path.Combine(monitorFolder, "A.txt");
                var fileName2 = Path.Combine(monitorFolder, "B.txt");
                var fileName3 = Path.Combine(monitorFolder, "C.txt");
                File.AppendAllText(fileName1, "ABCD");
                File.AppendAllText(fileName2, "ABCD EFGH");
                File.AppendAllText(fileName3, "ABCD EFGH IJKL");
            }

            management = new IndexManagement(codeIndexConfiguration, log1);
            management.AddIndex(indexConfig);
            var maintainer = management.GetIndexMaintainerWrapperAndInitializeIfNeeded(indexConfig.Pk);

            // Wait initialized finished
            while (maintainer.Result.Status == IndexStatus.Initializing_ComponentInitializeFinished || maintainer.Result.Status == IndexStatus.Initialized)
            {
                Thread.Sleep(100);
            }
        }

        public CodeIndexSearcher GetIndexSearcher()
        {
            return new CodeIndexSearcher(management, log2);
        }

        public Guid IndexPk => indexConfig.Pk;

        public void Dispose()
        {
            management.Dispose();
        }
    }
}
