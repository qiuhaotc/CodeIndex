using System.Linq;

namespace CodeIndex.Files
{
    public static class FilePathHelper
    {
        public static string[] GetPaths(string[] paths, bool isInLinux)
        {
            return isInLinux ? paths?.Select(u => u.ToUpperInvariant().Replace('\\', '/')).ToArray() : paths?.Select(u => u.ToUpperInvariant().Replace('/', '\\')).ToArray();
        }
    }
}
