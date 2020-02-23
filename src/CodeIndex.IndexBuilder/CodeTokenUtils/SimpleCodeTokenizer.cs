using System.IO;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Util;

namespace CodeIndex.IndexBuilder
{
    class SimpleCodeTokenizer : CharTokenizer
    {
        public SimpleCodeTokenizer(LuceneVersion matchVersion, TextReader input) : base(matchVersion, input)
        {
        }

        protected override bool IsTokenChar(int c)
        {
            return !char.IsWhiteSpace((char)c);
        }
    }
}
