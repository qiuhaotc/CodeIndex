using CodeIndex.Common;
using CodeIndex.IndexBuilder;
using CodeIndex.Search;
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
        protected QueryGenerator Generator => generator ??= new QueryGenerator();

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
