using System;
using System.IO;

namespace CodeIndex.Common
{
    public class IndexConfig
    {
        public const char SplitChar = '|';

        // TODO: Index Name readonly when edit, not allow special characters
        public string IndexName { get; set; }

        public string MonitorFolder { get; set; }
        public int MaxContentHighlightLength { get; set; } = 3000000;
        public int SaveIntervalSeconds { get; set; } = 300;
        public string OpenIDEUriFormat { get; set; }
        public string MonitorFolderRealPath { get; set; }

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

        public (string CodeIndexFolder, string HintIndexFolder) GetFolders(string parentFolder)
        {
            var rootFolder = GetRootFolder(parentFolder);
            return (Path.Combine(rootFolder, CodeIndexConfiguration.CodeIndexFolder), Path.Combine(rootFolder, CodeIndexConfiguration.HintIndexFolder));
        }

        public string GetRootFolder(string parentFolder)
        {
            return Path.Combine(parentFolder, CodeIndexConfiguration.CodeIndexesFolder, IndexName);
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
        string excludedPaths = "\\RELEASES\\|\\BIN\\|\\OBJ\\|\\DEBUGPUBLIC\\";
        string includedExtensions = ".CS|.XML|.XAML|.JS|.TXT|.SQL";
        string excludedExtensions = ".DLL|.PBD";
    }
}
