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

            Assert.That(wrapper.Status, Is.EqualTo(IndexStatus.Idle));
            Assert.That(wrapper.QueryGenerator, Is.Not.Null);
            Assert.That(wrapper.QueryParserNormal, Is.Not.Null);
            Assert.That(wrapper.QueryParserNormal.LowercaseExpandedTerms, Is.True);
            Assert.That(wrapper.QueryParserCaseSensitive, Is.Not.Null);
            Assert.That(wrapper.QueryParserCaseSensitive.LowercaseExpandedTerms, Is.False);
        }
    }
}
