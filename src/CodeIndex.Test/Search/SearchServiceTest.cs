﻿using System.Linq;
using CodeIndex.Common;
using CodeIndex.Search;
using Lucene.Net.Search;
using NUnit.Framework;

namespace CodeIndex.Test
{
    [Timeout(10000)]
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

            Assert.IsTrue(result.Status.Success);
            Assert.AreEqual(1, result.Result.Count());

            result = searchService.GetCodeSources(new SearchRequest
            {
                Content = "ABCD",
                ShowResults = 10,
                IndexPk = initManagement.IndexPk
            });
            Assert.AreEqual(3, result.Result.Count());
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

            Assert.IsTrue(result.Status.Success);
            Assert.AreEqual(1, result.Result.Count());

            result = searchService.GetCodeSourcesWithMatchedLine(new SearchRequest
            {
                Content = "ABCD",
                ShowResults = 10,
                IndexPk = initManagement.IndexPk
            });

            CollectionAssert.AreEquivalent(new[] { 1, 1, 1, 2 }, result.Result.Select(u => u.MatchedLine));
        }

        [Test]
        public void TestGetHints()
        {
            using var initManagement = new InitManagement(MonitorFolder, Config, true);
            var searcher = initManagement.GetIndexSearcher();
            var searchService = new SearchService(Config, new DummyLog<SearchService>(), searcher);
            var allwords = searcher.IndexManagement.GetIndexMaintainerWrapperAndInitializeIfNeeded(initManagement.IndexPk).Result.Maintainer.IndexBuilder.HintIndexPool.Search(new MatchAllDocsQuery(), 100).Select(u => u.Get(nameof(CodeWord.Word)));
            Assert.Multiple(() =>
            {
                CollectionAssert.AreEquivalent(new[] { "ABCD", "EFGH", "IJKL", "ABCE" }, allwords);

                var result = searchService.GetHints("abc", initManagement.IndexPk);
                Assert.IsTrue(result.Status.Success);
                CollectionAssert.AreEquivalent(new[] { "ABCD", "ABCE" }, result.Result);

                result = searchService.GetHints("efg", initManagement.IndexPk);
                Assert.IsTrue(result.Status.Success);
                CollectionAssert.AreEquivalent(new[] { "EFGH" }, result.Result);
            });
        }
    }
}
