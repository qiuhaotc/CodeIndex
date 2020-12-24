using System;
using System.IO;

namespace CodeIndex.Common
{
    public record IndexConfig
    {
        public const char SplitChar = '|';
        public const string FilePathPlaceholder = "{FilePath}";
        public const string LinePlaceholder = "{Line}";
        public const string ColumnPlaceholder = "{Column}";
        public Guid Pk { get; set; } = Guid.NewGuid();
        public string IndexName { get; set; } = string.Empty;
        public string MonitorFolder { get; set; } = string.Empty;
        public int MaxContentHighlightLength { get; set; } = 3000000;
        public int SaveIntervalSeconds { get; set; } = 3;
        public string OpenIDEUriFormat { get; set; } = $"vscode://file/{FilePathPlaceholder}:{LinePlaceholder}:{ColumnPlaceholder}";
        public string MonitorFolderRealPath { get; set; } = string.Empty;

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
            return Path.Combine(parentFolder, CodeIndexConfiguration.CodeIndexesFolder, Pk.ToString());
        }

        public void TrimValues()
        {
            IndexName = IndexName?.Trim();
            MonitorFolder = MonitorFolder?.Trim();
            OpenIDEUriFormat = OpenIDEUriFormat?.Trim();
            MonitorFolderRealPath = MonitorFolderRealPath?.Trim();
            ExcludedPaths = ExcludedPaths?.Trim();
            IncludedExtensions = IncludedExtensions?.Trim();
            ExcludedExtensions = ExcludedExtensions?.Trim();
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
        string excludedPaths = "\\RELEASES\\|\\BIN\\|\\OBJ\\|\\DEBUGPUBLIC\\|\\PACKAGES\\|\\.GIT\\";
        string includedExtensions = ".CS|.XML|.XAML|.JS|.TXT|.SQL|.CSPROJ|.SLN|.CSHTML|.RAZOR";
        string excludedExtensions = ".DLL|.PBD";
    }
}
