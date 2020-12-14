namespace CodeIndex.Common
{
    public class CodeIndexConfiguration
    {
        public const string CodeIndexesFolder = "CodeIndexes";
        public const string ConfigurationIndexFolder = "Configuration";
        public const string CodeIndexFolder = "CodeIndex";
        public const string HintIndexFolder = "HintIndex";

        public string LuceneIndex { get; set; } = string.Empty;
        public bool IsInLinux { get; set; }
        public string LocalUrl { get; set; } = string.Empty;
        public UserInfo[] ManagerUsers { get; set; }

        public int MaximumResults
        {
            get => maximumResults;
            set
            {
                value.RequireRange(nameof(maximumResults), 10000000, 100);

                maximumResults = value;
            }
        }

        int maximumResults = 10000;
    }
}
