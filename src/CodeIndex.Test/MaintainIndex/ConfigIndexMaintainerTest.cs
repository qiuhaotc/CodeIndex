using System.IO;
using CodeIndex.Common;
using CodeIndex.MaintainIndex;
using NUnit.Framework;

namespace CodeIndex.Test
{
    public class ConfigIndexMaintainerTest : BaseTest
    {
        [Test]
        public void TestConstructor()
        {
            using var maintainer = new ConfigIndexMaintainer(Config, Log);
            Assert.That(Directory.Exists(Path.Combine(Config.LuceneIndex, CodeIndexConfiguration.ConfigurationIndexFolder)), Is.True);
        }

        [Test]
        public void TestGetConfigs()
        {
            using var maintainer = new ConfigIndexMaintainer(Config, Log);
            Assert.That(maintainer.GetConfigs(), Is.Empty);

            var indexConfig = new IndexConfig
            {
                IndexName = "ABC"
            };

            maintainer.AddIndexConfig(indexConfig);

            Assert.That(maintainer.GetConfigs(), Is.EquivalentTo(new[] { indexConfig }));
        }

        [Test]
        public void TestAddConfig()
        {
            using var maintainer = new ConfigIndexMaintainer(Config, Log);
            Assert.That(maintainer.GetConfigs(), Is.Empty);

            var indexConfig1 = new IndexConfig
            {
                IndexName = "ABC"
            };

            var indexConfig2 = new IndexConfig
            {
                IndexName = "BCD"
            };

            maintainer.AddIndexConfig(indexConfig1);
            Assert.That(maintainer.GetConfigs(), Is.EquivalentTo(new[] { indexConfig1 }));

            maintainer.AddIndexConfig(indexConfig2);
            Assert.That(maintainer.GetConfigs(), Is.EquivalentTo(new[] { indexConfig1, indexConfig2 }));
        }

        [Test]
        public void TestEditConfig()
        {
            using var maintainer = new ConfigIndexMaintainer(Config, Log);
            var indexConfig = new IndexConfig
            {
                IndexName = "ABC"
            };

            maintainer.AddIndexConfig(indexConfig);
            Assert.That(maintainer.GetConfigs(), Is.EquivalentTo(new[] { indexConfig }));

            maintainer.EditIndexConfig(indexConfig with { IndexName = "EFG" });
            Assert.That(maintainer.GetConfigs(), Is.EquivalentTo(new[] { indexConfig with { IndexName = "EFG" } }));
        }

        [Test]
        public void TestDeleteConfig()
        {
            using var maintainer = new ConfigIndexMaintainer(Config, Log);
            var indexConfig = new IndexConfig
            {
                IndexName = "ABC"
            };

            maintainer.AddIndexConfig(indexConfig);
            Assert.That(maintainer.GetConfigs(), Is.EquivalentTo(new[] { indexConfig }));

            maintainer.DeleteIndexConfig(indexConfig.Pk);
            Assert.That(maintainer.GetConfigs(), Is.Empty);
        }

        [Test]
        public void TestIndexPersistent()
        {
            var indexConfig = new IndexConfig
            {
                IndexName = "ABC"
            };

            using (var maintainer = new ConfigIndexMaintainer(Config, Log))
            {
                maintainer.AddIndexConfig(indexConfig);
                Assert.That(maintainer.GetConfigs(), Is.EquivalentTo(new[] { indexConfig }));
            }

            using (var maintainer = new ConfigIndexMaintainer(Config, Log))
            {
                Assert.That(maintainer.GetConfigs(), Is.EquivalentTo(new[] { indexConfig }));
            }
        }
    }
}
