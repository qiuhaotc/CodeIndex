using System;

namespace CodeIndex.Common
{
    public class PendingRetrySource : ChangedSource
    {
        public int RetryTimes { get; set; }
        public DateTime LastRetryUTCDate { get; set; }

        public override string ToString()
        {
            return $"{base.ToString()} {RetryTimes} {LastRetryUTCDate}";
        }
    }
}