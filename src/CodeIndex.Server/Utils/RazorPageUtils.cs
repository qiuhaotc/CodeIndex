using CodeIndex.Common;

namespace CodeIndex.Server
{
    public static class RazorPageUtils
    {
        public static string GetOpenIDEUri(string openIDEUriFormat, string filePath, string monitorFolderRealPath)
        {
            if (!string.IsNullOrWhiteSpace(openIDEUriFormat))
            {
                return openIDEUriFormat.Replace("{FilePath}", GetPath(filePath, monitorFolderRealPath));
            }
            else
            {
                return filePath;
            }
        }

        static string GetPath(string path, string monitorFolderRealPath)
        {
            if (path != null && path.StartsWith("/monitorfolder") && !string.IsNullOrWhiteSpace(monitorFolderRealPath))
            {
                return monitorFolderRealPath + path.SubStringSafe("/monitorfolder".Length, path.Length);
            }
            else
            {
                return path;
            }
        }
    }
}
