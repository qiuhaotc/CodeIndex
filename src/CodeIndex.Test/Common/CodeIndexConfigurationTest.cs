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
                MaximumResults = 234,
                ManagerUsers = new[]
                {
                    new UserInfo
                    {
                        Id = 1,
                        Password = "ABC",
                        UserName = "DEF"
                    }
                }
            };

            Assert.Multiple(() =>
            {
                Assert.That(config.IsInLinux, Is.True);
                Assert.That(config.LocalUrl, Is.EqualTo("http://localhost:1234"));
                Assert.That(config.LuceneIndex, Is.EqualTo("AAA/BBB"));
                Assert.That(config.MaximumResults, Is.EqualTo(234));
                Assert.That(config.ManagerUsers, Is.EquivalentTo(new[] { new UserInfo
                    {
                        Id = 1,
                        Password = "ABC",
                        UserName = "DEF"
                    }
                }));
            });
        }
    }
}
