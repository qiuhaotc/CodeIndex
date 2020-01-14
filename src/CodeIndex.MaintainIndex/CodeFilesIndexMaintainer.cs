using System;
using System.IO;
using CodeIndex.Common;
using CodeIndex.Files;
using CodeIndex.IndexBuilder;
using Lucene.Net.Index;

namespace CodeIndex.MaintainIndex
{
    public class CodeFilesIndexMaintainer : IDisposable
    {
        public CodeFilesIndexMaintainer(string watchPath, string indexPath, string[] excludedPatterns, string[] excludedPaths, string includedPattern = "*")
        {
            watchPath.RequireNotNullOrEmpty(nameof(watchPath));
            excludedPatterns.RequireNotNull(nameof(watchPath));
            excludedPaths.RequireNotNull(nameof(excludedPaths));
            includedPattern.RequireNotNullOrEmpty(nameof(includedPattern));

            this.watchPath = watchPath;
            this.indexPath = indexPath;
            this.excludedPatterns = excludedPatterns;
            this.excludedPaths = excludedPaths;
            this.includedPattern = includedPattern;
            FileSystemWatcher = FilesWatcherHelper.StartWatch(watchPath, OnFileChange, RenamedEventHandler);
        }

        public void Dispose()
        {
            FileSystemWatcher.EnableRaisingEvents = false;
            FileSystemWatcher.Dispose();
        }

        public FileSystemWatcher FileSystemWatcher { get; private set; }

        string watchPath;
        string indexPath;
        string[] excludedPatterns;
        string[] excludedPaths;
        string includedPattern;

        void OnFileChange(object sender, FileSystemEventArgs e)
        {
            switch (e.ChangeType)
            {
                case WatcherChangeTypes.Changed:
                    CodeIndexBuilder.UpdateIndex(indexPath, new Term(nameof(CodeSource.FilePath), e.FullPath), CodeSource.GetCodeSource(new DirectoryInfo(e.FullPath)));
                    break;

                case WatcherChangeTypes.Created:
                    CodeIndexBuilder.BuildIndex(indexPath, false, false, CodeSource.GetCodeSource(new DirectoryInfo(e.FullPath)));
                    break;

                case WatcherChangeTypes.Deleted:
                    CodeIndexBuilder.DeleteIndex(indexPath, new Term(nameof(CodeSource.FilePath), e.FullPath));
                    break;
            }

            CodeIndexBuilder.CloseIndexWriterAndCommitChange(indexPath);
        }

        void RenamedEventHandler(object sender, RenamedEventArgs e)
        {
            switch (e.ChangeType)
            {
                case WatcherChangeTypes.Renamed:
                    CodeIndexBuilder.DeleteIndex(indexPath, new Term(nameof(CodeSource.FilePath), e.OldFullPath));
                    CodeIndexBuilder.BuildIndex(indexPath, false, false, CodeSource.GetCodeSource(new DirectoryInfo(e.FullPath)));
                    break;
            }

            CodeIndexBuilder.CloseIndexWriterAndCommitChange(indexPath);
        }
    }
}
