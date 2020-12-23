using System;
using System.Runtime.InteropServices;
using System.Web;
using CodeIndex.MaintainIndex;
using NUnit.Framework;

namespace CodeIndex.Test
{
    [Timeout(10000)]
    public class CodeIndexSearcherTest : BaseTest
    {
        [Test]
        public void TestSearchCode()
        {
            using var initManagement = new InitManagement(MonitorFolder, Config, true);
            var searcher = initManagement.GetIndexSearcher();

            var results1 = searcher.SearchCode("ABCD", out var query, 10, initManagement.IndexPk);
            Assert.That(results1.Length, Is.EqualTo(3));

            var results2 = searcher.SearchCode("ABCD", out query, 2, initManagement.IndexPk);
            Assert.That(results2.Length, Is.EqualTo(2));

            var results3 = searcher.SearchCode(QueryGenerator.GetSearchStr("EFGH", false), out query, 2, initManagement.IndexPk);
            Assert.That(results3.Length, Is.EqualTo(2));

            var results4 = searcher.SearchCode(QueryGenerator.GetSearchStr("\"A.txt\"", null, null, null), out query, 10, initManagement.IndexPk);
            Assert.That(results4.Length, Is.EqualTo(1));
            Assert.That(results4[0].FileName, Is.EqualTo("A.txt"));
        }

        [Test]
        public void TestGenerateHtmlPreviewText()
        {
            using var initManagement = new InitManagement(MonitorFolder, Config);
            var searcher = initManagement.GetIndexSearcher();

            var content = $"My ABC{Environment.NewLine}Is A ABC CONTENT{Environment.NewLine}It's abc in lowercase{Environment.NewLine}It's Abc in mix{Environment.NewLine}Not AB with C";
            var result = searcher.GenerateHtmlPreviewText("ABC", content, int.MaxValue, initManagement.IndexPk);
            Assert.AreEqual(@"My <label class='highlight'>ABC</label>
Is A <label class='highlight'>ABC</label> CONTENT
It&#39;s <label class='highlight'>abc</label> in lowercase
It&#39;s <label class='highlight'>Abc</label> in mix
Not AB with C", result);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                result = searcher.GenerateHtmlPreviewText("ABC", content, 10, initManagement.IndexPk);
                Assert.AreEqual(@"My <label class='highlight'>ABC</label>
Is A <label class='highlight'>ABC</label>...s <label class='highlight'>abc</label> in", result);
            }
        }

        [Test]
        public void TestGenerateHtmlPreviewText_ReturnRawContent()
        {
            using var initManagement = new InitManagement(MonitorFolder, Config);
            var searcher = initManagement.GetIndexSearcher();

            var content = $"My ABC{Environment.NewLine}Is A ABC CONTENT{Environment.NewLine}It's abc in lowercase{Environment.NewLine}It's Abc in mix{Environment.NewLine}Not AB with C";
            var result = searcher.GenerateHtmlPreviewText("NotExistWord", content, int.MaxValue, initManagement.IndexPk);
            Assert.IsEmpty(result);

            result = searcher.GenerateHtmlPreviewText("NotExistWord", content, 10, initManagement.IndexPk, returnRawContentWhenResultIsEmpty: true);
            Assert.AreEqual(HttpUtility.HtmlEncode(content), result);

            result = searcher.GenerateHtmlPreviewText(null, content, 10, initManagement.IndexPk, returnRawContentWhenResultIsEmpty: true);
            Assert.AreEqual(HttpUtility.HtmlEncode(content), result);
        }

        [Test]
        public void TestGenerateHtmlPreviewText_ContentTooLong()
        {
            using var initManagement = new InitManagement(MonitorFolder, Config, maxContentHighlightLength: 20);
            var searcher = initManagement.GetIndexSearcher();

            var content = $"My ABC{Environment.NewLine}Is A ABC CONTENT{Environment.NewLine}It's abc in lowercase{Environment.NewLine}It's Abc in mix{Environment.NewLine}Not AB with C";
            var result = searcher.GenerateHtmlPreviewText("ABC", content, int.MaxValue, initManagement.IndexPk);
            Assert.AreEqual(@"Content is too long to highlight", result);
        }

        [Test]
        public void TestGetHints()
        {
            using var initManagement = new InitManagement(MonitorFolder, Config, true);
            var searcher = initManagement.GetIndexSearcher();

            searcher.GetHints("ABC", initManagement.IndexPk);
            CollectionAssert.AreEquivalent(new[] { "ABCD", "ABCE" }, searcher.GetHints("Abc", initManagement.IndexPk));
            CollectionAssert.IsEmpty(searcher.GetHints("Abc", initManagement.IndexPk, caseSensitive: true));
            CollectionAssert.AreEquivalent(new[] { "EFGH" }, searcher.GetHints("EFG", initManagement.IndexPk));
        }

        [Test]
        public void TestGeneratePreviewTextWithLineNumber()
        {
            using var initManagement = new InitManagement(MonitorFolder, Config);
            var searcher = initManagement.GetIndexSearcher();

            var content = $"My ABC{Environment.NewLine}Is A ABC CONTENT{Environment.NewLine}ABCD EFG";
            var results = searcher.GeneratePreviewTextWithLineNumber(searcher.GetContentQueryFromStr("ABC", initManagement.IndexPk, false), content, int.MaxValue, 100, initManagement.IndexPk);
            Assert.That(results, Has.Length.EqualTo(2));
            Assert.AreEqual(("My <label class='highlight'>ABC</label>", 1), results[0]);
            Assert.AreEqual(("Is A <label class='highlight'>ABC</label> CONTENT", 2), results[1]);

            results = searcher.GeneratePreviewTextWithLineNumber(searcher.GetContentQueryFromStr("ABC", initManagement.IndexPk, false), content, int.MaxValue, 1, initManagement.IndexPk);
            Assert.That(results, Has.Length.EqualTo(1));
            Assert.AreEqual(("My <label class='highlight'>ABC</label>", 1), results[0]);
        }

        [Test]
        public void TestGeneratePreviewTextWithLineNumber_ContentTooLong()
        {
            using var initManagement = new InitManagement(MonitorFolder, Config, maxContentHighlightLength: 10);
            var searcher = initManagement.GetIndexSearcher();

            var content = $"My ABC{Environment.NewLine}Is A ABC CONTENT{Environment.NewLine}ABCD EFG";
            var results = searcher.GeneratePreviewTextWithLineNumber(searcher.GetContentQueryFromStr("ABC", initManagement.IndexPk, false), content, int.MaxValue, 100, initManagement.IndexPk);
            Assert.That(results, Has.Length.EqualTo(1));
            Assert.AreEqual(("Content is too long to highlight", 1), results[0]);
        }

        [Test]
        public void TestGeneratePreviewTextWithLineNumber_CompletionPrefixAndSuffix()
        {
            using var initManagement = new InitManagement(MonitorFolder, Config);
            var searcher = initManagement.GetIndexSearcher();

            var content = $"OH ABC{Environment.NewLine}DEF QWE ABC DEF ABC{Environment.NewLine}DEF OOOODD DEF ABC";
            var results = searcher.GeneratePreviewTextWithLineNumber(searcher.GetContentQueryFromStr("\"ABC DEF\"", initManagement.IndexPk, false), content, int.MaxValue, 100, initManagement.IndexPk);
            Assert.That(results, Has.Length.EqualTo(3));
            Assert.AreEqual(("OH <label class='highlight'>ABC</label>", 1), results[0]);
            Assert.AreEqual(("<label class='highlight'>DEF</label> QWE <label class='highlight'>ABC</label> <label class='highlight'>DEF</label> <label class='highlight'>ABC</label>", 2), results[1]);
            Assert.AreEqual(("<label class='highlight'>DEF</label> OOOODD DEF ABC", 3), results[2]);
        }

        [Test]
        public void TestGeneratePreviewTextWithLineNumber_TrimLine()
        {
            using var initManagement = new InitManagement(MonitorFolder, Config);
            var searcher = initManagement.GetIndexSearcher();

            var content = $"{Environment.NewLine}   \t\tABC\t   \t";
            var results = searcher.GeneratePreviewTextWithLineNumber(searcher.GetContentQueryFromStr("ABC", initManagement.IndexPk, false), content, int.MaxValue, 100, initManagement.IndexPk);
            Assert.That(results, Has.Length.EqualTo(1));
            Assert.AreEqual(("<label class='highlight'>ABC</label>", 2), results[0]);
        }
    }
}
