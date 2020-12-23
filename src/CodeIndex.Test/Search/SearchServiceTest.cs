using CodeIndex.Search;
using NUnit.Framework;

namespace CodeIndex.Test
{
    [Timeout(10000)]
    public class SearchServiceTest : BaseTest
    {
        [Test]
        public void TestSearchService()
        {
            using var initManagement = new InitManagement(MonitorFolder, Config, true);
            var searcher = initManagement.GetIndexSearcher();
            var searchService = new SearchService(Config, new DummyLog<SearchService>(), searcher);

            // TODO: Finish Unit Tests
        }
    }
}
