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
            DirectoryAssert.Exists(Path.Combine(Config.LuceneIndex, CodeIndexConfiguration.ConfigurationIndexFolder));
        }

        [Test]
        public void TestGetConfigs()
        {
            using var maintainer = new ConfigIndexMaintainer(Config, Log);
            CollectionAssert.IsEmpty(maintainer.GetConfigs());

            var indexConfig = new IndexConfig
            {
                IndexName = "ABC"
            };

            maintainer.AddIndexConfig(indexConfig);

            CollectionAssert.AreEquivalent(new[] { indexConfig }, maintainer.GetConfigs());
        }

        [Test]
        public void TestAddConfig()
        {
            using var maintainer = new ConfigIndexMaintainer(Config, Log);
            CollectionAssert.IsEmpty(maintainer.GetConfigs());

            var indexConfig1 = new IndexConfig
            {
                IndexName = "ABC"
            };

            var indexConfig2 = new IndexConfig
            {
                IndexName = "BCD"
            };

            maintainer.AddIndexConfig(indexConfig1);
            CollectionAssert.AreEquivalent(new[] { indexConfig1 }, maintainer.GetConfigs());

            maintainer.AddIndexConfig(indexConfig2);
            CollectionAssert.AreEquivalent(new[] { indexConfig1, indexConfig2 }, maintainer.GetConfigs());
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
            CollectionAssert.AreEquivalent(new[] { indexConfig }, maintainer.GetConfigs());

            maintainer.EditIndexConfig(indexConfig with { IndexName = "EFG" });
            CollectionAssert.AreEquivalent(new[] { indexConfig with { IndexName = "EFG" } }, maintainer.GetConfigs());
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
            CollectionAssert.AreEquivalent(new[] { indexConfig }, maintainer.GetConfigs());

            maintainer.DeleteIndexConfig(indexConfig.Pk);
            CollectionAssert.IsEmpty(maintainer.GetConfigs());
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
                CollectionAssert.AreEquivalent(new[] { indexConfig }, maintainer.GetConfigs());
            }

            using (var maintainer = new ConfigIndexMaintainer(Config, Log))
            {
                CollectionAssert.AreEquivalent(new[] { indexConfig }, maintainer.GetConfigs());
            }
        }
    }
}
