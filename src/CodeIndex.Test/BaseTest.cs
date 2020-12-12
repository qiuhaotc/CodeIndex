using CodeIndex.Common;
using CodeIndex.IndexBuilder;
using CodeIndex.MaintainIndex;
using Lucene.Net.QueryParsers.Classic;
using NUnit.Framework;

namespace CodeIndex.Test
{
    public class BaseTest : BaseTestLight
    {
        public CodeIndexConfiguration Config => new CodeIndexConfiguration
        {
            LuceneIndex = TempIndexDir,
            MonitorFolder = MonitorFolder
        };

        QueryGenerator generator;
        protected QueryGenerator Generator => generator ??= new QueryGenerator(new QueryParser(Constants.AppLuceneVersion, nameof(CodeSource.Content), new CodeAnalyzer(Constants.AppLuceneVersion, true)));

        [SetUp]
        protected override void SetUp()
        {
            base.SetUp();
            WordsHintBuilder.Words.Clear();
        }

        [TearDown]
        protected override void TearDown()
        {
            LucenePool.SaveResultsAndClearLucenePool(Config);
            WordsHintBuilder.Words.Clear();

            base.TearDown();
        }
    }
}
