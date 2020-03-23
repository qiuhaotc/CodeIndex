using System.Linq;
using CodeIndex.Common;

namespace CodeIndex.Files
{
    public static class FilePathHelper
    {
        public static string[] GetPaths(string[] paths, bool isInLinux)
        {
            if (isInLinux)
            {
                return paths?.Select(u => u.ToUpperInvariant().Replace('\\', '/')).ToArray();
            }

            return paths?.Select(u => u.ToUpperInvariant().Replace('/', '\\')).ToArray();
        }
    }
}
