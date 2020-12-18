using System.Collections.Generic;
using System.IO;
using CodeIndex.Common;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.Miscellaneous;
using Lucene.Net.Util;

namespace CodeIndex.IndexBuilder
{
    public class CodeAnalyzer : Analyzer
    {
        readonly LuceneVersion luceneVersion;
        readonly bool lowerCase;

        public CodeAnalyzer(LuceneVersion luceneVersion, bool lowerCase)
        {
            this.luceneVersion = luceneVersion;
            this.lowerCase = lowerCase;
        }

        protected override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
        {
            var tokenizer = new CodeTokenizer(reader);

            if (lowerCase)
            {
                return new TokenStreamComponents(tokenizer, new LowerCaseFilter(luceneVersion, tokenizer));
            }

            return new TokenStreamComponents(tokenizer);
        }

        public static Analyzer GetCaseSensitiveAndInsesitiveCodeAnalyzer(params string[] fieldNamesNeedCaseSensitive)
        {
            fieldNamesNeedCaseSensitive.RequireContainsElement(nameof(fieldNamesNeedCaseSensitive));

            Dictionary<string, Analyzer> analyzerPerField = new();
            var caseSensitiveAnalyzer = new CodeAnalyzer(Constants.AppLuceneVersion, false);

            foreach (var fieldNameNeedCaseSensitive in fieldNamesNeedCaseSensitive)
            {
                analyzerPerField.Add(fieldNameNeedCaseSensitive, caseSensitiveAnalyzer);
            }

            var analyzer = new PerFieldAnalyzerWrapper(new CodeAnalyzer(Constants.AppLuceneVersion, true), analyzerPerField);
            return analyzer;
        }
    }
}
