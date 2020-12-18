using Lucene.Net.Util;

namespace CodeIndex
{
    public class Constants
    {
        public const LuceneVersion AppLuceneVersion = LuceneVersion.LUCENE_48;

        public const int ReadWriteLockTimeOutMilliseconds = 60000; // 60 seconds

        public const string NoneTokenizeFieldSuffix = "NoneTokenize";

        public const string CaseSensitive = "CaseSensitive";

        public const int DefaultMaxContentHighlightLength = 3000000;
    }
}
