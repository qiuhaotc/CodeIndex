using System.IO;

namespace CodeIndex.Common
{
    public class CodeIndexConfiguration
    {
        public string LuceneIndex { get; set; }
        public string MonitorFolder { get; set; }
        public string LuceneIndexForCode => luceneIndexForCode ??= Path.Combine(LuceneIndex, "CodeIndex");
        public string LuceneIndexForHint => luceneIndexForHint ??= Path.Combine(LuceneIndex, "HintIndex");

        string luceneIndexForCode;
        string luceneIndexForHint;
    }
}
