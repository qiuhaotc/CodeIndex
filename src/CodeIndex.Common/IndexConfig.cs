using System;
using System.Collections.Generic;

namespace CodeIndex.Common
{
    public class IndexConfig
    {
        public Guid Pk { get; set; }
        public string IndexName { get; set; }
        public string MonitorFolder { get; set; }
        public IEnumerable<string> IncludedExtensions { get; set; }
        public IEnumerable<string> ExcludedExtensions { get; set; }
        public int MaxContentHighlightLength { get; set; }
        public IEnumerable<string> ExcludedPaths { get; set; }
        public int SaveIntervalSeconds { get; set; }
        public string OpenIDEUriFormat { get; set; }
        public string MonitorFolderRealPath { get; set; }
        public DateTime IndexCreatedDate { get; set; }
        public DateTime IndexLastUpdatedDate { get; set; }
    }
}
