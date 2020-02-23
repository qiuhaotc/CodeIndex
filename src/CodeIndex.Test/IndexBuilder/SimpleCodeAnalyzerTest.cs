using System.Collections.Generic;
using System.Linq;
using CodeIndex.IndexBuilder;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.TokenAttributes;
using NUnit.Framework;

namespace CodeIndex.Test
{
    public class SimpleCodeAnalyzerTest
    {
        [Test]
        public void TestAnalyzer()
        {
            var content = " LucenePool.SaveResultsAndClearLucenePool(TempIndexDir);";
            var processedContent = SimpleCodeContentProcessing.Preprocessing(content);
            var result = GetTokens(new SimpleCodeAnalyzer(Constants.AppLuceneVersion, false), processedContent, true);
            CollectionAssert.AreEquivalent(new[] 
            {
                "LucenePool",
                ".",
                "SaveResultsAndClearLucenePool",
                "(",
                "TempIndexDir",
                ")",
                ";"
            }, result);

            result = GetTokens(new SimpleCodeAnalyzer(Constants.AppLuceneVersion, true), processedContent, true);
            CollectionAssert.AreEquivalent(new[]
            {
                "lucenepool",
                ".",
                "saveresultsandclearlucenepool",
                "(",
                "tempindexdir",
                ")",
                ";"
            }, result);
        }

        List<string> GetTokens(Analyzer analyzer, string content, bool needRestoreString = false)
        {
            var tokens = new List<string>();
            var tokenStream = analyzer.GetTokenStream("A", content ?? string.Empty);
            var termAttr = tokenStream.GetAttribute<ICharTermAttribute>();
            tokenStream.Reset();

            while (tokenStream.IncrementToken())
            {
                tokens.Add(termAttr.ToString());
            }

            return needRestoreString ? tokens.Select(u => SimpleCodeContentProcessing.RestoreString(u)).ToList() : tokens;
        }
    }
}
