using System.Collections.Generic;
using System.IO;
using System.Linq;
using CodeIndex.Common;

namespace CodeIndex.Files
{
    public class FilesFetcher
    {
        public static IEnumerable<FileInfo> FetchAllFiles(string path, string[] excludedExtensions, string[] excludedPaths, string includedExtenstion = "*", string[] includedExtensions = null)
        {
            path.RequireNotNullOrEmpty(nameof(path));
            excludedExtensions.RequireNotNull(nameof(path));
            excludedPaths.RequireNotNull(nameof(excludedPaths));
            includedExtenstion.RequireNotNullOrEmpty(nameof(includedExtenstion));

            return Directory.GetFiles(path, includedExtenstion, SearchOption.AllDirectories)
	            .Where(f => !excludedExtensions.Any(extenstion => f.EndsWith(extenstion, System.StringComparison.InvariantCultureIgnoreCase))
	                        && !excludedPaths.Any(filePath => f.ToUpper().Contains(filePath))
	                        && (includedExtensions == null || includedExtensions.Any(extension =>f.EndsWith(extension, System.StringComparison.InvariantCultureIgnoreCase))))
                .Select(u => new FileInfo(u));
        }
    }
}
