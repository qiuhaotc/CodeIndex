using System.Linq;
using CodeIndex.Common;
using CodeIndex.IndexBuilder;
using Lucene.Net.Search;
using NUnit.Framework;

namespace CodeIndex.Test
{
    public class WordsHintBuilderTest : BaseTest
    {
        [Test]
        public void TestAddWords()
        {
            WordsHintBuilder.AddWords(new[] { "AAAA", "BBBB", "Ddddd", "AAAA", "EEE" });
            Assert.AreEqual(3, WordsHintBuilder.Words.Count, "Length must larger than 3 and skip duplicate");
            CollectionAssert.AreEquivalent(new[] { "AAAA", "BBBB", "Ddddd" }, WordsHintBuilder.Words);
        }

        [Test]
        public void TestBuildIndexByBatch()
        {
            WordsHintBuilder.AddWords(new[] { "AAAA", "Bbbb", "DDDDD" });
            WordsHintBuilder.BuildIndexByBatch(Config, true, true, true, null, true);

            var docs = LucenePool.Search(Config.LuceneIndexForHint, new MatchAllDocsQuery(), 1000);
            Assert.AreEqual(3, docs.Length);
            CollectionAssert.AreEquivalent(new[] { "AAAA", "Bbbb", "DDDDD" }, docs.Select(u => u.Get(nameof(CodeWord.Word))));
            CollectionAssert.AreEquivalent(new[] { "aaaa", "bbbb", "ddddd" }, docs.Select(u => u.Get(nameof(CodeWord.WordLower))));
            Assert.AreEqual(0, WordsHintBuilder.Words.Count);
        }

        [Test]
        public void TestBuildIndexByBatch_FirstInit()
        {
            WordsHintBuilder.AddWords(new[] { "AAAA", "Bbbb", "DDDDD" });
            WordsHintBuilder.BuildIndexByBatch(Config, true, true, true, null, true);

            var docs = LucenePool.Search(Config.LuceneIndexForHint, new MatchAllDocsQuery(), 1000);
            Assert.AreEqual(3, docs.Length);
            CollectionAssert.AreEquivalent(new[] { "AAAA", "Bbbb", "DDDDD" }, docs.Select(u => u.Get(nameof(CodeWord.Word))));

            WordsHintBuilder.AddWords(new[] { "AAAA" });
            WordsHintBuilder.BuildIndexByBatch(Config, true, true, true, null, true);
            docs = LucenePool.Search(Config.LuceneIndexForHint, new MatchAllDocsQuery(), 1000);
            Assert.AreEqual(4, docs.Length, "When is first init, not do update documents");
        }

        [Test]
        public void TestBuildIndexByBatch_NotFirstInit()
        {
            WordsHintBuilder.AddWords(new[] { "AAAA", "Bbbb", "DDDDD" });
            WordsHintBuilder.BuildIndexByBatch(Config, true, true, true, null, true);

            var docs = LucenePool.Search(Config.LuceneIndexForHint, new MatchAllDocsQuery(), 1000);
            Assert.AreEqual(3, docs.Length);
            CollectionAssert.AreEquivalent(new[] { "AAAA", "Bbbb", "DDDDD" }, docs.Select(u => u.Get(nameof(CodeWord.Word))));

            WordsHintBuilder.AddWords(new[] { "AAAA" });
            WordsHintBuilder.BuildIndexByBatch(Config, true, true, true, null, false);
            docs = LucenePool.Search(Config.LuceneIndexForHint, new MatchAllDocsQuery(), 1000);
            Assert.AreEqual(3, docs.Length, "When is not first init, do update documents");
        }

        [Test]
        public void TestUpdateWordsAndUpdateIndex()
        {
            WordsHintBuilder.AddWords(new[] { "AAAA", "Bbbbb", "DDDDD" });
            WordsHintBuilder.BuildIndexByBatch(Config, true, true, true, null, true);

            var docs = LucenePool.Search(Config.LuceneIndexForHint, new MatchAllDocsQuery(), 1000);
            Assert.AreEqual(3, docs.Length);

            WordsHintBuilder.UpdateWordsAndUpdateIndex(Config, new[] { "AAAA", "Bbbbb", "EEEEE", "ABC" }, null);
            docs = LucenePool.Search(Config.LuceneIndexForHint, new MatchAllDocsQuery(), 1000);
            Assert.AreEqual(4, docs.Length, "Skip duplicate and length muse larger than 3");
            CollectionAssert.AreEquivalent(new[] { "AAAA", "Bbbbb", "DDDDD", "EEEEE" }, docs.Select(u => u.Get(nameof(CodeWord.Word))));
            CollectionAssert.AreEquivalent(new[] { "aaaa", "bbbbb", "ddddd", "eeeee" }, docs.Select(u => u.Get(nameof(CodeWord.WordLower))));
        }
    }
}
