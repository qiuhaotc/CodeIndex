using CodeIndex.Common;
using static CodeIndex.Common.IndexConfig;

namespace CodeIndex.Server
{
    public static class RazorPageUtils
    {
        public static string GetOpenIDEUri(string openIDEUriFormat, string filePath, string monitorFolderRealPath, int line = 0, int column = 0)
        {
            if (!string.IsNullOrWhiteSpace(openIDEUriFormat))
            {
                return openIDEUriFormat.Replace(FilePathPlaceholder, GetPath(filePath, monitorFolderRealPath)).Replace(LinePlaceholder, line.ToString()).Replace(ColumnPlaceholder, column.ToString());
            }

            return filePath;
        }

        static string GetPath(string path, string monitorFolderRealPath)
        {
            if (path != null && path.StartsWith("/monitorfolder") && !string.IsNullOrWhiteSpace(monitorFolderRealPath))
            {
                return monitorFolderRealPath + path.SubStringSafe("/monitorfolder".Length, path.Length);
            }

            return path;
        }
    }
}
