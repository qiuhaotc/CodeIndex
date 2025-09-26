using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using CodeIndex.IndexBuilder;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.TokenAttributes;
using NUnit.Framework;

namespace CodeIndex.Test
{
    [ExcludeFromCodeCoverage]
    public class CodeAnalyzerTest
    {
        [Test]
        public void TestAnalyzer()
        {
            var content = " LucenePool.SaveResultsAndClearLucenePool(TempIndexDir);";
            var result = GetTokens(new CodeAnalyzer(Constants.AppLuceneVersion, false), content);
            Assert.That(result, Is.EquivalentTo(new[]
            {
                "LucenePool",
                ".",
                "SaveResultsAndClearLucenePool",
                "(",
                "TempIndexDir",
                ")",
                ";"
            }));

            result = GetTokens(new CodeAnalyzer(Constants.AppLuceneVersion, true), content);
            Assert.That(result, Is.EquivalentTo(new[]
            {
                "lucenepool",
                ".",
                "saveresultsandclearlucenepool",
                "(",
                "tempindexdir",
                ")",
                ";"
            }));

            result = GetTokens(new CodeAnalyzer(Constants.AppLuceneVersion, false), @"Line One
Line Two

Line Four");

            Assert.That(result, Is.EquivalentTo(new[]
            {
                "Line",
                "One",
                "Line",
                "Two",
                "Line",
                "Four"
            }));
        }

        [Test]
        public void TestGetWords()
        {
            var content = "It's a content for test" + Environment.NewLine + "这是一个例句,我知道了";
            Assert.That(WordSegmenter.GetWords(content), Is.EquivalentTo(new[] { "It", "s", "a", "content", "for", "test", "这是一个例句", "我知道了" }));
            Assert.That(WordSegmenter.GetWords(content, 2, 4), Is.EquivalentTo(new[] { "It", "for", "test", "我知道了" }));
            Assert.That(WordSegmenter.GetWords("a".PadRight(201, 'b')), Is.Empty);

            Assert.Throws<ArgumentException>(() => WordSegmenter.GetWords(null));
            Assert.Throws<ArgumentException>(() => WordSegmenter.GetWords(content, 0));
            Assert.Throws<ArgumentException>(() => WordSegmenter.GetWords(content, 200));
            Assert.Throws<ArgumentException>(() => WordSegmenter.GetWords(content, 3, 1));
            Assert.Throws<ArgumentException>(() => WordSegmenter.GetWords(content, 3, -1));
            Assert.Throws<ArgumentException>(() => WordSegmenter.GetWords(content, 3, 1001));
        }

        List<string> GetTokens(Analyzer analyzer, string content)
        {
            var tokens = new List<string>();
            using var tokenStream = analyzer.GetTokenStream("dummy", content);
            var termAttribute = tokenStream.AddAttribute<ICharTermAttribute>();
            tokenStream.Reset();
            while (tokenStream.IncrementToken())
            {
                tokens.Add(termAttribute.ToString());
            }
            tokenStream.End();
            return tokens;
        }
    }
}
