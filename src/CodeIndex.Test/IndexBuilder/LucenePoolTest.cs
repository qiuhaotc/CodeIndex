using CodeIndex.IndexBuilder;
using NUnit.Framework;

namespace CodeIndex.Test
{
    public class LucenePoolTest
    {
        [Test]
        public void Test()
        {
            Assert.NotNull(LucenePool.GetStandardParser());
        }
    }
}
