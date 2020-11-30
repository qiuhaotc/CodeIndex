using System;
using System.IO;

namespace CodeIndex.Common
{
    public class CodeIndexConfiguration
    {
        public const char SplitChar = '|';
        public const string CodeIndexesFolder = "CodeIndexes";
        public const string ConfigurationIndexFolder = "Configuration";

        public string LuceneIndex { get; set; } = string.Empty;
        public string MonitorFolder { get; set; } = string.Empty;
        public int MaxContentHighlightLength { get; set; } = 3000000;
        public string OpenIDEUriFormat { get; set; } = string.Empty;
        public bool IsInLinux { get; set; }
        public int SaveIntervalSeconds { get; set; } = 300;
        public string LocalUrl { get; set; } = string.Empty;
        public string MonitorFolderRealPath { get; set; } = string.Empty;
        public string LuceneIndexForCode => luceneIndexForCode ??= GetIndexPath("CodeIndex");
        public string LuceneIndexForHint => luceneIndexForHint ??= GetIndexPath("HintIndex");
        public string ExcludedExtensions { get; set; } = string.Empty;
        public string ExcludedPaths { get; set; } = string.Empty;
        public string IncludedExtensions { get; set; } = string.Empty;
        public string[] ExcludedExtensionsArray => excludedExtensionsArray ??= GetSplitStringArray(ExcludedExtensions);
        public string[] ExcludedPathsArray => excludedPathsArray ??= GetSplitStringArray(ExcludedPaths);
        public string[] IncludedExtensionsArray => includedExtensionsArray ??= GetSplitStringArray(IncludedExtensions);

        public int MaximumResults
        {
            get => maximumResults;
            set
            {
                value.RequireRange(nameof(maximumResults), 10000000, 100);

                maximumResults = value;
            }
        }

        string luceneIndexForCode;
        string luceneIndexForHint;
        string[] excludedExtensionsArray;
        string[] excludedPathsArray;
        string[] includedExtensionsArray;
        int maximumResults = 10000;

        string GetIndexPath(string path)
        {
            return Path.Combine(LuceneIndex, path);
        }

        string[] GetSplitStringArray(string excludedExtensions)
        {
            if (string.IsNullOrEmpty(excludedExtensions))
            {
                return Array.Empty<string>();
            }

            return excludedExtensions.Split(SplitChar, StringSplitOptions.RemoveEmptyEntries);
        }

        string luceneConfigurationIndex;
        public string LuceneConfigurationIndex => luceneConfigurationIndex ??= GetIndexPath(ConfigurationIndexFolder);
    }
}
