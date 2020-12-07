using System;
using System.IO;

namespace CodeIndex.MaintainIndex
{
    public class PendingRetrySource : ChangedSource
    {
        public int RetryTimes { get; set; }
        public DateTime LastRetryUTCDate { get; set; }
    }
}