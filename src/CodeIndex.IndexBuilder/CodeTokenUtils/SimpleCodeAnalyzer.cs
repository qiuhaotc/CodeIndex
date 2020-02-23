using System.IO;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Core;
using Lucene.Net.Util;

namespace CodeIndex.IndexBuilder
{
    public class SimpleCodeAnalyzer : Analyzer
    {
        readonly LuceneVersion luceneVersion;
        readonly bool lowerCase;

        public SimpleCodeAnalyzer(LuceneVersion luceneVersion, bool lowerCase)
        {
            this.luceneVersion = luceneVersion;
            this.lowerCase = lowerCase;
        }

        protected override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
        {
            var tokenizer = new SimpleCodeTokenizer(luceneVersion, reader);

            if (lowerCase)
            {
                return new TokenStreamComponents(tokenizer, new LowerCaseFilter(luceneVersion, tokenizer));
            }

            return new TokenStreamComponents(tokenizer);
        }
    }
}
