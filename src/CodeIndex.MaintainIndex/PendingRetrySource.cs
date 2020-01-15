using System;
using System.IO;

namespace CodeIndex.MaintainIndex
{
    class PendingRetrySource
    {
        public string FilePath { get; set; }
        public string OldPath { get; set; }
        public int RetryTimes { get; set; }
        public DateTime LastRetryUTCDate { get; set; }
        public WatcherChangeTypes ChangesType { get; set; }
    }
}