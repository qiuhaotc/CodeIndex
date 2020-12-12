using CodeIndex.Common;
using NUnit.Framework;

namespace CodeIndex.Test
{
    public class CodeIndexConfigurationTest
    {
        [Test]
        public void TestProperties()
        {
            var config = new CodeIndexConfiguration()
            {
                IsInLinux = true,
                LocalUrl = "http://localhost:1234",
                LuceneIndex = "AAA/BBB",
                MaximumResults = 234
            };

            Assert.Multiple(() =>
            {
                Assert.AreEqual(true, config.IsInLinux);
                Assert.AreEqual("http://localhost:1234", config.LocalUrl);
                Assert.AreEqual("AAA/BBB", config.LuceneIndex);
                Assert.AreEqual(234, config.MaximumResults);
            });
        }
    }
}
