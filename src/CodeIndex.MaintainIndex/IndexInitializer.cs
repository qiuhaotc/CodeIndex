using System;
using System.IO;
using System.Linq;
using CodeIndex.Common;
using CodeIndex.Files;
using CodeIndex.IndexBuilder;

namespace CodeIndex.MaintainIndex
{
    public class IndexInitializer
    {
        public void InitializeIndex(string codeFolder, string luceneIndex)
        {
            codeFolder.RequireNotNull(nameof(codeFolder));
            luceneIndex.RequireNotNull(nameof(luceneIndex));

            if (Directory.Exists(luceneIndex))
            {
                CodeIndexBuilder.DeleteAllIndex(luceneIndex);
            }
            else
            {
                Directory.CreateDirectory(luceneIndex);
            }

            CodeIndexBuilder.BuildIndex(luceneIndex, true, true, true, FilesFetcher.FetchAllFiles(codeFolder, Array.Empty<string>(), Array.Empty<string>()).Select(u => CodeSource.GetCodeSource(u, File.ReadAllText(u.FullName, FilesEncodingHelper.GetEncoding(u.FullName)))).ToArray());
            LucenePool.SaveLuceneResultsAndCloseIndexWriter(luceneIndex);
        }
    }
}
