using System;
using System.IO;

namespace CodeIndex.MaintainIndex
{
    public class PendingRetrySource
    {
        public string FilePath { get; set; }
        public string OldPath { get; set; }
        public int RetryTimes { get; set; }
        public DateTime LastRetryUTCDate { get; set; }
        public WatcherChangeTypes ChangesType { get; set; }
    }
}