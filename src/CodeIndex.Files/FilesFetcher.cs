using System.Collections.Generic;
using System.IO;
using System.Linq;
using CodeIndex.Common;

namespace CodeIndex.Files
{
    public class FilesFetcher
    {
        public static IEnumerable<FileInfo> FetchAllFiles(string path, string[] excludedExtenstions, string[] excludedPaths, string includedExtenstion = "*")
        {
            path.RequireNotNullOrEmpty(nameof(path));
            excludedExtenstions.RequireNotNull(nameof(path));
            excludedPaths.RequireNotNull(nameof(excludedPaths));
            includedExtenstion.RequireNotNullOrEmpty(nameof(includedExtenstion));

            return Directory.GetFiles(path, includedExtenstion, SearchOption.AllDirectories)
                .Select(u => new FileInfo(u))
                .Where(f => excludedExtenstions.All(extenstion => !f.Extension.Contains(extenstion)))
                .Where(f => excludedPaths.All(excluded => !f.DirectoryName.Contains(excluded)));
        }
    }
}
