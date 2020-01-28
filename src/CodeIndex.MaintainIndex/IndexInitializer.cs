using System.IO;
using System.Linq;
using CodeIndex.Common;
using CodeIndex.Files;
using CodeIndex.IndexBuilder;

namespace CodeIndex.MaintainIndex
{
    public class IndexInitializer
    {
        private ILog log;

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
                CodeIndexBuilder.DeleteAllIndex(luceneIndex);
            }
            else
            {
                log?.Info("Create index");
                Directory.CreateDirectory(luceneIndex);
            }

            CodeIndexBuilder.BuildIndex(luceneIndex, true, true, true, FilesFetcher.FetchAllFiles(codeFolder, excludedExtensions, excludedPaths, includedExtenstion, includedExtensions).Select(u => CodeSource.GetCodeSource(u, File.ReadAllText(u.FullName, FilesEncodingHelper.GetEncoding(u.FullName)))).ToArray());
            LucenePool.SaveLuceneResultsAndCloseIndexWriter(luceneIndex);
            log?.Info("Index initialized");
        }
    }
}
