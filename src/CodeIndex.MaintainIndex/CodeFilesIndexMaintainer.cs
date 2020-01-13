using System;
using System.IO;
using CodeIndex.Common;
using CodeIndex.Files;

namespace CodeIndex.MaintainIndex
{
    public class CodeFilesIndexMaintainer : IDisposable
    {
        public CodeFilesIndexMaintainer(string path, string[] excludedPatterns, string[] excludedPaths, string includedPattern = "*")
        {
            path.RequireNotNullOrEmpty(nameof(path));
            excludedPatterns.RequireNotNull(nameof(path));
            excludedPaths.RequireNotNull(nameof(excludedPaths));
            includedPattern.RequireNotNullOrEmpty(nameof(includedPattern));

            this.path = path;
            this.excludedPatterns = excludedPatterns;
            this.excludedPaths = excludedPaths;
            this.includedPattern = includedPattern;
            FileSystemWatcher = FilesWatcherHelper.StartWatch(path, OnFileChange, RenamedEventHandler);
        }

        public void Dispose()
        {
            FileSystemWatcher.EnableRaisingEvents = false;
            FileSystemWatcher.Dispose();
        }

        public FileSystemWatcher FileSystemWatcher { get; private set; }

        string path;
        string[] excludedPatterns;
        string[] excludedPaths;
        string includedPattern;

        void OnFileChange(object sender, FileSystemEventArgs e)
        {
            switch (e.ChangeType)
            {
                case WatcherChangeTypes.Changed:
                    break;

                case WatcherChangeTypes.Created:
                    break;

                case WatcherChangeTypes.Deleted:
                    break;

                default:
                    break;
            }
        }

        void RenamedEventHandler(object sender, RenamedEventArgs e)
        {
        }
    }
}
