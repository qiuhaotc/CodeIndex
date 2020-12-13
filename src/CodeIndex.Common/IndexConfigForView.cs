using System;

namespace CodeIndex.Common
{
    public class IndexConfigForView
    {
        public string IndexName { get; set; }
        public int MaxContentHighlightLength { get; set; }
        public int SaveIntervalSeconds { get; set; }
        public string OpenIDEUriFormat { get; set; }
        public string MonitorFolderRealPath { get; set; }
        public Guid Pk { get; set; }

        public static IndexConfigForView GetIndexConfigForView(IndexConfig indexConfig)
        {
            indexConfig.RequireNotNull(nameof(indexConfig));

            return new IndexConfigForView
            {
                MaxContentHighlightLength = indexConfig.MaxContentHighlightLength,
                SaveIntervalSeconds = indexConfig.SaveIntervalSeconds,
                OpenIDEUriFormat = indexConfig.OpenIDEUriFormat,
                MonitorFolderRealPath = indexConfig.MonitorFolderRealPath,
                IndexName = indexConfig.IndexName,
                Pk = indexConfig.Pk
            };
        }
    }
}
