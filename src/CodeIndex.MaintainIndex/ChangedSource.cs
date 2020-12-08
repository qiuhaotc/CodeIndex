using System;
using System.IO;

namespace CodeIndex.MaintainIndex
{
    public class ChangedSource
    {
        public string FilePath { get; set; }
        public string OldPath { get; set; }
        public WatcherChangeTypes ChangesType { get; set; }
        public DateTime ChangedUTCDate { get; } = DateTime.UtcNow;
    }
}
