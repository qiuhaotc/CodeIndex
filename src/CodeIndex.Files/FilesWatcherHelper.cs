using System.IO;
using CodeIndex.Common;

namespace CodeIndex.Files
{
    public static class FilesWatcherHelper
    {
        public static FileSystemWatcher StartWatch(string path, FileSystemEventHandler onchangedHandler, RenamedEventHandler onRenameHandler)
        {
            path.RequireNotNullOrEmpty(nameof(path));
            onchangedHandler.RequireNotNull(nameof(onchangedHandler));
            onRenameHandler.RequireNotNull(nameof(onRenameHandler));

            var watcher = new FileSystemWatcher();
            watcher.Path = path;
            watcher.NotifyFilter = NotifyFilters.LastWrite
                                 | NotifyFilters.FileName
                                 | NotifyFilters.DirectoryName;

            watcher.Filter = "*.*";

            // Add event handlers.
            watcher.Changed += onchangedHandler;
            watcher.Created += onchangedHandler;
            watcher.Deleted += onchangedHandler;
            watcher.Renamed += onRenameHandler;

            // Begin watching.
            watcher.EnableRaisingEvents = true;

            return watcher;
        }
    }
}
