using CodeIndex.Common;
using CodeIndex.MaintainIndex;
using NUnit.Framework;

namespace CodeIndex.Test
{
    public class IndexMaintainerWrapperTest : BaseTest
    {
        [Test]
        public void TestConstructor()
        {
            using var wrapper = new IndexMaintainerWrapper(new IndexConfig
            {
                IndexName ="AAA"
            }, Config, Log);

            Assert.AreEqual(IndexStatus.Idle, wrapper.Status);
            Assert.NotNull(wrapper.QueryGenerator);
            Assert.NotNull(wrapper.QueryParserNormal);
            Assert.IsTrue(wrapper.QueryParserNormal.LowercaseExpandedTerms);
            Assert.NotNull(wrapper.QueryParserCaseSensitive);
            Assert.IsFalse(wrapper.QueryParserCaseSensitive.LowercaseExpandedTerms);
        }
    }
}
