using System;
using System.IO;

namespace CodeIndex.Common
{
    public class IndexConfig
    {
        public const char SplitChar = '|';

        public Guid Pk { get; set; }
        public string IndexName { get; set; }
        public string MonitorFolder { get; set; }
        public int MaxContentHighlightLength { get; set; }
        public int SaveIntervalSeconds { get; set; }
        public string OpenIDEUriFormat { get; set; }
        public string MonitorFolderRealPath { get; set; }
        public DateTime IndexCreatedDate { get; set; }
        public DateTime IndexLastUpdatedDate { get; set; }

        public string ExcludedPaths
        {
            get => excludedPaths;
            set
            {
                excludedPaths = value;
                excludedPathsArray = null;
            }
        }

        public string IncludedExtensions
        {
            get => includedExtensions;
            set
            {
                includedExtensions = value;
                includedExtensionsArray = null;
            }
        }

        public string ExcludedExtensions
        {
            get => excludedExtensions;
            set
            {
                excludedExtensions = value;
                excludedExtensionsArray = null;
            }
        }

        public string[] ExcludedPathsArray => excludedPathsArray ??= GetSplitStringArray(ExcludedPaths);

        public string[] IncludedExtensionsArray => includedExtensionsArray ??= GetSplitStringArray(IncludedExtensions);

        public string[] ExcludedExtensionsArray => excludedExtensionsArray ??= GetSplitStringArray(ExcludedExtensions);

        public (string CodeIndexFolder,string HintIndexFolder) GetFolders(string parentFolder)
        {
            return (Path.Combine(parentFolder, IndexName, CodeIndexConfiguration.CodeIndexFolder), Path.Combine(parentFolder, IndexName, CodeIndexConfiguration.HintIndexFolder));
        }

        string[] GetSplitStringArray(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return Array.Empty<string>();
            }

            return value.Split(SplitChar, StringSplitOptions.RemoveEmptyEntries);
        }

        string[] excludedPathsArray;
        string[] includedExtensionsArray;
        string[] excludedExtensionsArray;
        string excludedPaths;
        string includedExtensions;
        string excludedExtensions;
    }
}
