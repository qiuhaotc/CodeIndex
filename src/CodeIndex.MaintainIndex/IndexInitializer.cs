using System.IO;
using System.Linq;
using CodeIndex.Common;
using CodeIndex.Files;
using CodeIndex.IndexBuilder;

namespace CodeIndex.MaintainIndex
{
    public class IndexInitializer
    {
        readonly ILog log;

        public IndexInitializer(ILog log = null)
        {
            this.log = log;
        }

        public void InitializeIndex(string codeFolder, string luceneIndex, string[] excludedExtensions, string[] excludedPaths, string includedExtenstion = "*", string[] includedExtensions = null)
        {
            codeFolder.RequireNotNull(nameof(codeFolder));
            luceneIndex.RequireNotNull(nameof(luceneIndex));

            if (Directory.Exists(luceneIndex))
            {
                log?.Info("Delete exist index");
                // TODO: Not delete, update index and delete not exists one
                CodeIndexBuilder.DeleteAllIndex(luceneIndex);
            }
            else
            {
                log?.Info("Create index");
                Directory.CreateDirectory(luceneIndex);
            }

            // TODO: Add retry logic like maintainer
            CodeIndexBuilder.BuildIndex(luceneIndex, true, true, true, FilesFetcher.FetchAllFiles(codeFolder, excludedExtensions, excludedPaths, includedExtenstion, includedExtensions).Select(u => CodeSource.GetCodeSource(u, FilesContentHelper.ReadAllText(u.FullName))));
            LucenePool.SaveResultsAndClearLucenePool(luceneIndex);
            log?.Info("Index initialized");
        }
    }
}
