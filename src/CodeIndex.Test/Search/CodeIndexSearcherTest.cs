using System;
using System.Runtime.InteropServices;
using System.Web;
using CodeIndex.Common;
using CodeIndex.MaintainIndex;
using NUnit.Framework;

namespace CodeIndex.Test
{
    [CancelAfter(10000)]
    public class CodeIndexSearcherTest : BaseTest
    {
        [Test]
        public void TestSearchCode()
        {
            using var initManagement = new InitManagement(MonitorFolder, Config, true);
            var searcher = initManagement.GetIndexSearcher();

            var results1 = searcher.SearchCode(GetSearchRequest("ABCD", initManagement.IndexPk, showResults: 10));
            Assert.That(results1.Length, Is.EqualTo(3));

            var results2 = searcher.SearchCode(GetSearchRequest("ABCD", initManagement.IndexPk, showResults: 2));
            Assert.That(results2.Length, Is.EqualTo(2));

            var results3 = searcher.SearchCode(GetSearchRequest("EFGH", initManagement.IndexPk));
            Assert.That(results3.Length, Is.EqualTo(2));

            var results4 = searcher.SearchCode(GetSearchRequest(null, initManagement.IndexPk, "\"A.txt\""));
            Assert.That(results4.Length, Is.EqualTo(1));
            Assert.That(results4[0].FileName, Is.EqualTo("A.txt"));
        }

        [Test]
        public void TestGenerateHtmlPreviewText()
        {
            using var initManagement = new InitManagement(MonitorFolder, Config);
            var searcher = initManagement.GetIndexSearcher();

            var content = $"My ABC{Environment.NewLine}Is A ABC CONTENT{Environment.NewLine}It's abc in lowercase{Environment.NewLine}It's Abc in mix{Environment.NewLine}Not AB with C";
            var result = searcher.GenerateHtmlPreviewText(GetSearchRequest("ABC", initManagement.IndexPk), content, int.MaxValue);
            Assert.That(result, Is.EqualTo(@"My <span class='highlight'>ABC</span>
Is A <span class='highlight'>ABC</span> CONTENT
It&#39;s <span class='highlight'>abc</span> in lowercase
It&#39;s <span class='highlight'>Abc</span> in mix
Not AB with C"));

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                result = searcher.GenerateHtmlPreviewText(GetSearchRequest("ABC", initManagement.IndexPk), content, 10);
                Assert.That(result, Is.EqualTo(@"My <span class='highlight'>ABC</span>
Is A <span class='highlight'>ABC</span>...s <span class='highlight'>abc</span> in"));
            }
        }

        [Test]
        public void TestGenerateHtmlPreviewText_ReturnRawContent()
        {
            using var initManagement = new InitManagement(MonitorFolder, Config);
            var searcher = initManagement.GetIndexSearcher();

            var content = $"My ABC{Environment.NewLine}Is A ABC CONTENT{Environment.NewLine}It's abc in lowercase{Environment.NewLine}It's Abc in mix{Environment.NewLine}Not AB with C";
            var result = searcher.GenerateHtmlPreviewText(GetSearchRequest("NotExistWord", initManagement.IndexPk), content, int.MaxValue);
            Assert.That(result, Is.Empty);

            result = searcher.GenerateHtmlPreviewText(GetSearchRequest("NotExistWord", initManagement.IndexPk), content, 10, returnRawContentWhenResultIsEmpty: true);
            Assert.That(result, Is.EqualTo(HttpUtility.HtmlEncode(content)));

            result = searcher.GenerateHtmlPreviewText(null, content, 10, returnRawContentWhenResultIsEmpty: true);
            Assert.That(result, Is.EqualTo(HttpUtility.HtmlEncode(content)));
        }

        [Test]
        public void TestGenerateHtmlPreviewText_ContentTooLong()
        {
            using var initManagement = new InitManagement(MonitorFolder, Config, maxContentHighlightLength: 20);
            var searcher = initManagement.GetIndexSearcher();

            var content = $"My ABC{Environment.NewLine}Is A ABC CONTENT{Environment.NewLine}It's abc in lowercase{Environment.NewLine}It's Abc in mix{Environment.NewLine}Not AB with C";
            var result = searcher.GenerateHtmlPreviewText(GetSearchRequest("ABC", initManagement.IndexPk), content, int.MaxValue);
            Assert.That(result, Is.EqualTo(@"Content is too long to highlight"));
        }

        [Test]
        public void TestGetHints()
        {
            using var initManagement = new InitManagement(MonitorFolder, Config, true);
            var searcher = initManagement.GetIndexSearcher();

            searcher.GetHints("ABC", initManagement.IndexPk);
            Assert.That(searcher.GetHints("Abc", initManagement.IndexPk), Is.EquivalentTo(new[] { "ABCD", "ABCE" }));
            Assert.That(searcher.GetHints("Abc", initManagement.IndexPk, caseSensitive: true), Is.Empty);
            Assert.That(searcher.GetHints("EFG", initManagement.IndexPk), Is.EquivalentTo(new[] { "EFGH" }));
        }

        [Test]
        public void TestGeneratePreviewTextWithLineNumber()
        {
            using var initManagement = new InitManagement(MonitorFolder, Config);
            var searcher = initManagement.GetIndexSearcher();

            var content = $"My ABC{Environment.NewLine}Is A ABC CONTENT{Environment.NewLine}ABCD EFG";
            var results = searcher.GeneratePreviewTextWithLineNumber(searcher.GetContentQuery(GetSearchRequest("ABC", initManagement.IndexPk)), content, int.MaxValue, 100, initManagement.IndexPk);
            Assert.That(results, Has.Length.EqualTo(2));
            Assert.That(results[0], Is.EqualTo(("My <span class='highlight'>ABC</span>", 1)));
            Assert.That(results[1], Is.EqualTo(("Is A <span class='highlight'>ABC</span> CONTENT", 2)));

            results = searcher.GeneratePreviewTextWithLineNumber(searcher.GetContentQuery(GetSearchRequest("ABC", initManagement.IndexPk)), content, int.MaxValue, 1, initManagement.IndexPk);
            Assert.That(results, Has.Length.EqualTo(1));
            Assert.That(results[0], Is.EqualTo(("My <span class='highlight'>ABC</span>", 1)));
        }

        [Test]
        public void TestGeneratePreviewTextWithLineNumber_ContentTooLong()
        {
            using var initManagement = new InitManagement(MonitorFolder, Config, maxContentHighlightLength: 10);
            var searcher = initManagement.GetIndexSearcher();

            var content = $"My ABC{Environment.NewLine}Is A ABC CONTENT{Environment.NewLine}ABCD EFG";
            var results = searcher.GeneratePreviewTextWithLineNumber(searcher.GetContentQuery(GetSearchRequest("ABC", initManagement.IndexPk)), content, int.MaxValue, 100, initManagement.IndexPk);
            Assert.That(results, Has.Length.EqualTo(1));
            Assert.That(results[0], Is.EqualTo(("Content is too long to highlight", 1)));
        }

        [Test]
        public void TestGeneratePreviewTextWithLineNumber_CompletionPrefixAndSuffix()
        {
            using var initManagement = new InitManagement(MonitorFolder, Config);
            var searcher = initManagement.GetIndexSearcher();

            var content = $"OH ABC{Environment.NewLine}DEF QWE ABC DEF ABC{Environment.NewLine}DEF OOOODD DEF ABC";
            var results = searcher.GeneratePreviewTextWithLineNumber(searcher.GetContentQuery(GetSearchRequest("\"ABC DEF\"", initManagement.IndexPk)), content, int.MaxValue, 100, initManagement.IndexPk);
            Assert.That(results, Has.Length.EqualTo(3));
            Assert.That(results[0], Is.EqualTo(("OH <span class='highlight'>ABC</span>", 1)));
            Assert.That(results[1], Is.EqualTo(("<span class='highlight'>DEF</span> QWE <span class='highlight'>ABC</span> <span class='highlight'>DEF</span> <span class='highlight'>ABC</span>", 2)));
            Assert.That(results[2], Is.EqualTo(("<span class='highlight'>DEF</span> OOOODD DEF ABC", 3)));
        }

        [Test]
        public void TestGeneratePreviewTextWithLineNumber_TrimLine()
        {
            using var initManagement = new InitManagement(MonitorFolder, Config);
            var searcher = initManagement.GetIndexSearcher();

            var content = $"{Environment.NewLine}   \t\tABC\t   \t";
            var results = searcher.GeneratePreviewTextWithLineNumber(searcher.GetContentQuery(GetSearchRequest("ABC", initManagement.IndexPk)), content, int.MaxValue, 100, initManagement.IndexPk);
            Assert.That(results, Has.Length.EqualTo(1));
            Assert.That(results[0], Is.EqualTo(("<span class='highlight'>ABC</span>", 2)));
        }

        SearchRequest GetSearchRequest(string content, Guid indexPk, string fileName = null, int showResults = 20) => new SearchRequest { Content = content, IndexPk = indexPk, FileName = fileName, ShowResults = showResults };
    }
}
