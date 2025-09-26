using System.Linq;
using CodeIndex.Common;
using CodeIndex.Search;
using NUnit.Framework;

namespace CodeIndex.Test
{
    [CancelAfter(10000)]
    public class SearchServiceTest : BaseTest
    {
        [Test]
        public void TestGetCodeSources()
        {
            using var initManagement = new InitManagement(MonitorFolder, Config, true);
            var searcher = initManagement.GetIndexSearcher();
            var searchService = new SearchService(Config, new DummyLog<SearchService>(), searcher);

            var result = searchService.GetCodeSources(new SearchRequest
            {
                Content = "ABCD",
                ShowResults = 1,
                IndexPk = initManagement.IndexPk
            });

            Assert.That(result.Status.Success, Is.True);
            Assert.That(result.Result.Count(), Is.EqualTo(1));

            result = searchService.GetCodeSources(new SearchRequest
            {
                Content = "ABCD",
                ShowResults = 10,
                IndexPk = initManagement.IndexPk
            });
            Assert.That(result.Result.Count(), Is.EqualTo(3));
        }

        [Test]
        public void TestGetCodeSourcesWithMatchedLine()
        {
            using var initManagement = new InitManagement(MonitorFolder, Config, true);
            var searcher = initManagement.GetIndexSearcher();
            var searchService = new SearchService(Config, new DummyLog<SearchService>(), searcher);

            var result = searchService.GetCodeSourcesWithMatchedLine(new SearchRequest
            {
                Content = "ABCD",
                ShowResults = 1,
                IndexPk = initManagement.IndexPk
            });

            Assert.That(result.Status.Success, Is.True);
            Assert.That(result.Result.Count(), Is.EqualTo(1));

            result = searchService.GetCodeSourcesWithMatchedLine(new SearchRequest
            {
                Content = "ABCD",
                ShowResults = 10,
                IndexPk = initManagement.IndexPk
            });

            Assert.That(result.Result.Select(u => u.MatchedLine), Is.EquivalentTo(new[] { 1, 1, 1, 2 }));
        }

        [Test]
        public void TestGetHints()
        {
            using var initManagement = new InitManagement(MonitorFolder, Config, true);
            var searcher = initManagement.GetIndexSearcher();
            var searchService = new SearchService(Config, new DummyLog<SearchService>(), searcher);
            var result = searchService.GetHints("abc", initManagement.IndexPk);
            Assert.That(result.Status.Success, Is.True);
            Assert.That(result.Result, Is.EquivalentTo(new[] { "ABCD", "ABCE" }));

            result = searchService.GetHints("efg", initManagement.IndexPk);
            Assert.That(result.Status.Success, Is.True);
            Assert.That(result.Result, Is.EquivalentTo(new[] { "EFGH" }));
        }

        [Test]
        public void TestSearchExtension()
        {
            using var initManagement = new InitManagement(MonitorFolder, Config, true);
            var searcher = initManagement.GetIndexSearcher();
            var searchService = new SearchService(Config, new DummyLog<SearchService>(), searcher);

            var result = searchService.GetCodeSources(new SearchRequest
            {
                FileExtension = "TXT",
                ShowResults = int.MaxValue,
                IndexPk = initManagement.IndexPk,
                CaseSensitive = true
            });

            Assert.That(result.Status.Success, Is.True);
            Assert.That(result.Result.Count(), Is.EqualTo(3), "Extension is case-insensitive");

            result = searchService.GetCodeSources(new SearchRequest
            {
                FileExtension = "txt",
                ShowResults = int.MaxValue,
                IndexPk = initManagement.IndexPk,
                CaseSensitive = true
            });

            Assert.That(result.Result.Count(), Is.EqualTo(3), "Extension is case-insensitive");

            result = searchService.GetCodeSources(new SearchRequest
            {
                FileExtension = "sql",
                ShowResults = int.MaxValue,
                IndexPk = initManagement.IndexPk,
                CaseSensitive = true
            });

            Assert.That(result.Result.Count(), Is.EqualTo(0));
        }
    }
}
