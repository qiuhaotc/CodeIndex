using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CodeIndex.Common;

namespace CodeIndex.Files
{
    public class FilesFetcher
    {
        public static IEnumerable<FileInfo> FetchAllFiles(string path, string[] excludedExtensions, string[] excludedPaths, string includedExtenstion = "*", string[] includedExtensions = null, bool isInLinux = false)
        {
            path.RequireNotNullOrEmpty(nameof(path));
            excludedExtensions.RequireNotNull(nameof(path));
            excludedPaths.RequireNotNull(nameof(excludedPaths));
            includedExtenstion.RequireNotNullOrEmpty(nameof(includedExtenstion));

            excludedPaths = FilePathHelper.GetPaths(excludedPaths, isInLinux);
            excludedExtensions = excludedExtensions.Select(u => u.ToUpperInvariant()).ToArray();
            includedExtensions = includedExtensions?.Select(u => u.ToUpperInvariant()).ToArray() ?? Array.Empty<string>();

            return Directory.GetFiles(path, includedExtenstion, SearchOption.AllDirectories)
                .Where(f => !excludedExtensions.Any(extenstion => f.EndsWith(extenstion, StringComparison.InvariantCultureIgnoreCase))
                            && !excludedPaths.Any(filePath => f.ToUpperInvariant().Contains(filePath))
                            && (includedExtensions.Length == 0 || includedExtensions.Any(extension => f.EndsWith(extension, StringComparison.InvariantCultureIgnoreCase))))
                .Select(u => new FileInfo(u));
        }
    }
}
