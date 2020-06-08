using System;
using System.Web;
using CodeIndex.Common;
using CodeIndex.IndexBuilder;
using CodeIndex.Search;
using Lucene.Net.Index;
using Lucene.Net.Search;
using NUnit.Framework;

namespace CodeIndex.Test
{
    public class CodeIndexSearcherTest : BaseTest
    {
        [Test]
        public void TestSearch_NewReader()
        {
            CodeIndexBuilder.BuildIndex(Config, true, true, true, new[] { new CodeSource
            {
                FileName = "Dummy File 1",
                FileExtension = "cs",
                FilePath = @"C:\Dummy File 1.cs",
                Content = "Test Content" + Environment.NewLine + "A New Line For Test"
            },
            new CodeSource
            {
                FileName = "Dummy File 2",
                FileExtension = "cs",
                FilePath = @"C:\Dummy File 2.cs",
                Content = "Test Content" + Environment.NewLine + "A New Line For Test"
            },
            new CodeSource
            {
                FileName = "Dummy File 2",
                FileExtension = "xml",
                FilePath = @"C:\Dummy File.xml",
                Content = "Test Content" + Environment.NewLine + "A New Line For Test"
            }});

            LucenePool.SaveResultsAndClearLucenePool(Config.LuceneIndexForCode);

            var results1 = CodeIndexSearcher.Search(Config.LuceneIndexForCode, new TermQuery(new Term(nameof(CodeSource.FileExtension), "cs")), 10);
            Assert.That(results1.Length, Is.EqualTo(2));

            var results2 = CodeIndexSearcher.Search(Config.LuceneIndexForCode, new TermQuery(new Term(nameof(CodeSource.FileExtension), "cs")), 1);
            Assert.That(results2.Length, Is.EqualTo(1));
        }

        [Test]
        public void TestSearch_ReaderFromWriter()
        {
            CodeIndexBuilder.BuildIndex(Config, true, true, true, new[] { new CodeSource
            {
                FileName = "Dummy File 1",
                FileExtension = "cs",
                FilePath = @"C:\Dummy File 1.cs",
                Content = "Test Content" + Environment.NewLine + "A New Line For Test"
            },
            new CodeSource
            {
                FileName = "Dummy File 2",
                FileExtension = "cs",
                FilePath = @"C:\Dummy File 2.cs",
                Content = "Test Content" + Environment.NewLine + "A New Line For Test"
            },
            new CodeSource
            {
                FileName = "Dummy File 2",
                FileExtension = "xml",
                FilePath = @"C:\Dummy File.xml",
                Content = "Test Content" + Environment.NewLine + "A New Line For Test"
            }});

            var results = CodeIndexSearcher.Search(Config.LuceneIndexForCode, new TermQuery(new Term(nameof(CodeSource.FileExtension), "xml")), 10);
            Assert.That(results.Length, Is.EqualTo(1));
        }

        [Test]
        public void TestGenerateHtmlPreviewText()
        {
            var generator = new QueryGenerator();
            var content = $"My ABC{Environment.NewLine}Is A ABC CONTENT{Environment.NewLine}It's abc in lowercase{Environment.NewLine}It's Abc in mix{Environment.NewLine}Not AB with C";
            var result = CodeIndexSearcher.GenerateHtmlPreviewText(generator.GetQueryFromStr("ABC"), content, int.MaxValue, LucenePool.GetAnalyzer());
            Assert.AreEqual(@"My <label class='highlight'>ABC</label>
Is A <label class='highlight'>ABC</label> CONTENT
It&#39;s <label class='highlight'>abc</label> in lowercase
It&#39;s <label class='highlight'>Abc</label> in mix
Not AB with C", result);

            result = CodeIndexSearcher.GenerateHtmlPreviewText(generator.GetQueryFromStr("ABC"), content, 10, LucenePool.GetAnalyzer());
            Assert.AreEqual(@"My <label class='highlight'>ABC</label>
Is A <label class='highlight'>ABC</label>...
It&#39;s <label class='highlight'>Abc</label>", result);
        }

        [Test]
        public void TestGenerateHtmlPreviewText_ReturnRawContent()
        {
            var generator = new QueryGenerator();
            var content = $"My ABC{Environment.NewLine}Is A ABC CONTENT{Environment.NewLine}It's abc in lowercase{Environment.NewLine}It's Abc in mix{Environment.NewLine}Not AB with C";
            var result = CodeIndexSearcher.GenerateHtmlPreviewText(generator.GetQueryFromStr("NotExistWord"), content, int.MaxValue, LucenePool.GetAnalyzer());
            Assert.IsEmpty(result);

            result = CodeIndexSearcher.GenerateHtmlPreviewText(generator.GetQueryFromStr("NotExistWord"), content, 10, LucenePool.GetAnalyzer(), returnRawContentWhenResultIsEmpty: true);
            Assert.AreEqual(HttpUtility.HtmlEncode(content), result);

            result = CodeIndexSearcher.GenerateHtmlPreviewText(null, content, 10, LucenePool.GetAnalyzer(), returnRawContentWhenResultIsEmpty: true);
            Assert.AreEqual(HttpUtility.HtmlEncode(content), result);
        }

        [Test]
        public void TestGenerateHtmlPreviewText_ContentTooLong()
        {
            var generator = new QueryGenerator();
            var content = $"My ABC{Environment.NewLine}Is A ABC CONTENT{Environment.NewLine}It's abc in lowercase{Environment.NewLine}It's Abc in mix{Environment.NewLine}Not AB with C";
            var result = CodeIndexSearcher.GenerateHtmlPreviewText(generator.GetQueryFromStr("ABC"), content, int.MaxValue, LucenePool.GetAnalyzer(), maxContentHighlightLength: 20);
            Assert.AreEqual(@"Content is too long to highlight", result);
        }

        [Test]
        public void TestSearchCode()
        {
            CodeIndexBuilder.BuildIndex(Config, true, true, true, new[] { new CodeSource
            {
                FileName = "Dummy File 1",
                FileExtension = "cs",
                FilePath = @"C:\Dummy File 1.cs",
                Content = "Test Content" + Environment.NewLine + "A New Line For Test"
            }});

            var results = CodeIndexSearcher.SearchCode(Config.LuceneIndexForCode, new TermQuery(new Term(nameof(CodeSource.FileExtension), "cs")), 10);
            Assert.That(results.Length, Is.EqualTo(1));
        }

        [Test]
        public void TestGetHints()
        {
            WordsHintBuilder.Words.Add("Abcd");
            WordsHintBuilder.BuildIndexByBatch(Config, true, true, true, null, true, 1000);

            CollectionAssert.AreEquivalent(new[] { "Abcd" }, CodeIndexSearcher.GetHints(Config.LuceneIndexForHint, "abc", 20, false));
            CollectionAssert.IsEmpty(CodeIndexSearcher.GetHints(Config.LuceneIndexForHint, "abc", 20, true));
            CollectionAssert.AreEquivalent(new[] { "Abcd" }, CodeIndexSearcher.GetHints(Config.LuceneIndexForHint, "Abc", 20, true));
        }

        [Test]
        public void TestGeneratePreviewTextWithLineNumber()
        {
            var generator = new QueryGenerator();
            var content = $"My ABC{Environment.NewLine}Is A ABC CONTENT{Environment.NewLine}ABCD EFG";
            var results = CodeIndexSearcher.GeneratePreviewTextWithLineNumber(generator.GetQueryFromStr("ABC"), content, int.MaxValue, LucenePool.GetAnalyzer(), 100);
            Assert.That(results, Has.Length.EqualTo(2));
            Assert.AreEqual(("My <label class='highlight'>ABC</label>", 1), results[0]);
            Assert.AreEqual(("Is A <label class='highlight'>ABC</label> CONTENT", 2), results[1]);

            results = CodeIndexSearcher.GeneratePreviewTextWithLineNumber(generator.GetQueryFromStr("ABC"), content, int.MaxValue, LucenePool.GetAnalyzer(), 1);
            Assert.That(results, Has.Length.EqualTo(1));
            Assert.AreEqual(("My <label class='highlight'>ABC</label>", 1), results[0]);
        }

        [Test]
        public void TestGeneratePreviewTextWithLineNumber_ContentTooLong()
        {
            var generator = new QueryGenerator();
            var content = $"My ABC{Environment.NewLine}Is A ABC CONTENT{Environment.NewLine}ABCD EFG";
            var results = CodeIndexSearcher.GeneratePreviewTextWithLineNumber(generator.GetQueryFromStr("ABC"), content, int.MaxValue, LucenePool.GetAnalyzer(), 100, 10);
            Assert.That(results, Has.Length.EqualTo(1));
            Assert.AreEqual(("Content is too long to highlight", 1), results[0]);
        }

        [Test]
        public void TestGeneratePreviewTextWithLineNumber_CompletionPrefixAndSuffix()
        {
            var generator = new QueryGenerator();
            var content = $"OH ABC{Environment.NewLine}DEF QWE ABC DEF ABC{Environment.NewLine}DEF OOOODD DEF ABC";
            var results = CodeIndexSearcher.GeneratePreviewTextWithLineNumber(generator.GetQueryFromStr("\"ABC DEF\""), content, int.MaxValue, LucenePool.GetAnalyzer(), 100);
            Assert.That(results, Has.Length.EqualTo(3));
            Assert.AreEqual(("OH <label class='highlight'>ABC</label>", 1), results[0]);
            Assert.AreEqual(("<label class='highlight'>DEF</label> QWE <label class='highlight'>ABC</label> <label class='highlight'>DEF</label> <label class='highlight'>ABC</label>", 2), results[1]);
            Assert.AreEqual(("<label class='highlight'>DEF</label> OOOODD DEF ABC", 3), results[2]);
        }

        [Test]
        public void TestGeneratePreviewTextWithLineNumber_TrimLine()
        {
            var generator = new QueryGenerator();
            var content = $"{Environment.NewLine}   \t\tABC\t   \t";
            var results = CodeIndexSearcher.GeneratePreviewTextWithLineNumber(generator.GetQueryFromStr("ABC"), content, int.MaxValue, LucenePool.GetAnalyzer(), 100);
            Assert.That(results, Has.Length.EqualTo(1));
            Assert.AreEqual(("<label class='highlight'>ABC</label>", 2), results[0]);
        }
    }
}
