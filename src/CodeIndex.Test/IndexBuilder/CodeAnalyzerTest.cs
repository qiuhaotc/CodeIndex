using System.Collections.Generic;
using System.Linq;
using CodeIndex.IndexBuilder;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.TokenAttributes;
using NUnit.Framework;

namespace CodeIndex.Test.IndexBuilder
{
    public class CodeAnalyzerTest
    {
        [Test]
        public void TestAnalyzer()
        {
            var content = " LucenePool.SaveResultsAndClearLucenePool(TempIndexDir);";
            var result = GetTokens(new CodeAnalyzer(Constants.AppLuceneVersion, false), content);
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

            result = GetTokens(new CodeAnalyzer(Constants.AppLuceneVersion, true), content);
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

            result = GetTokens(new CodeAnalyzer(Constants.AppLuceneVersion, false), @"Line One
Line Two

Line Four");

            CollectionAssert.AreEquivalent(new[]
            {
                "Line",
                "One",
                "Line",
                "Two",
                "Line",
                "Four"
            }, result);
        }

        List<string> GetTokens(Analyzer analyzer, string content)
        {
            var tokens = new List<string>();
            var tokenStream = analyzer.GetTokenStream("A", content);
            var termAttr = tokenStream.GetAttribute<ICharTermAttribute>();
            tokenStream.Reset();

            while (tokenStream.IncrementToken())
            {
                tokens.Add(termAttr.ToString());
            }

            return tokens;
        }
    }
}
