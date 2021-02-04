using System.IO;
using CodeIndex.Common;

namespace CodeIndex.Files
{
    public static class FilesWatcherHelper
    {
        public static FileSystemWatcher StartWatch(string path, FileSystemEventHandler onChangedHandler, RenamedEventHandler onRenameHandler)
        {
            path.RequireNotNullOrEmpty(nameof(path));
            onChangedHandler.RequireNotNull(nameof(onChangedHandler));
            onRenameHandler.RequireNotNull(nameof(onRenameHandler));

            var watcher = new FileSystemWatcher();
            watcher.Path = path;
            watcher.NotifyFilter = NotifyFilters.DirectoryName |
                                   NotifyFilters.LastWrite |
                                   NotifyFilters.FileName |
                                   NotifyFilters.Size;
            //No need: NotifyFilters.LastAccess, NotifyFilters.Attributes

            watcher.Filter = "*.*";

            // Add event handlers.
            watcher.Changed += onChangedHandler;
            watcher.Created += onChangedHandler;
            watcher.Deleted += onChangedHandler;
            watcher.Renamed += onRenameHandler;

            // Include Sub Dir
            watcher.IncludeSubdirectories = true;

            // Begin watching.
            watcher.EnableRaisingEvents = true;

            return watcher;
        }
    }
}
