using System;
using System.Linq;
using CodeIndex.Common;
using CodeIndex.IndexBuilder;
using NUnit.Framework;

namespace CodeIndex.Test
{
    public class ConfigIndexBuilderTest : BaseTest
    {
        [Test]
        public void TestGetConfigs()
        {
            using var configBuilder = new ConfigIndexBuilder(TempConfigDir);
            configBuilder.AddIndexConfig(new IndexConfig
            {
                IndexName = "ABC"
            });

            Assert.AreEqual("ABC", configBuilder.GetConfigs().First().IndexName);
        }

        [Test]
        public void TestAddIndexConfig()
        {
            using var configBuilder = new ConfigIndexBuilder(TempConfigDir);
            configBuilder.AddIndexConfig(new IndexConfig
            {
                IndexName = "ABC"
            });

            Assert.AreEqual("ABC", configBuilder.GetConfigs().First().IndexName);

            configBuilder.AddIndexConfig(new IndexConfig
            {
                IndexName = "EFG"
            });

            CollectionAssert.AreEquivalent(new[] { "ABC", "EFG" }, configBuilder.GetConfigs().Select(u => u.IndexName));
        }

        [Test]
        public void TestDeleteIndexConfig()
        {
            using var configBuilder = new ConfigIndexBuilder(TempConfigDir);

            var config1 = new IndexConfig
            {
                IndexName = "ABC"
            };

            var config2 = new IndexConfig
            {
                IndexName = "EFG"
            };

            configBuilder.AddIndexConfig(config1);
            configBuilder.AddIndexConfig(config2);
            Assert.AreEqual(2, configBuilder.GetConfigs().Count());

            configBuilder.DeleteIndexConfig(config2.Pk);
            CollectionAssert.AreEquivalent(new[] { "ABC" }, configBuilder.GetConfigs().Select(u => u.IndexName));

            configBuilder.DeleteIndexConfig(config1.Pk);
            Assert.AreEqual(0, configBuilder.GetConfigs().Count());
        }

        [Test]
        public void TestEditIndexConfig()
        {
            using var configBuilder = new ConfigIndexBuilder(TempConfigDir);

            var config1 = new IndexConfig
            {
                IndexName = "ABC"
            };

            var config2 = new IndexConfig
            {
                IndexName = "EFG"
            };

            configBuilder.AddIndexConfig(config1);
            configBuilder.AddIndexConfig(config2);
            Assert.AreEqual(2, configBuilder.GetConfigs().Count());

            config1.IndexName = "NEW";
            configBuilder.EditIndexConfig(config1);
            CollectionAssert.AreEquivalent(new[] { "NEW", "EFG" }, configBuilder.GetConfigs().Select(u => u.IndexName));

            config2.IndexName = "NEW NEW";
            configBuilder.EditIndexConfig(config2);
            CollectionAssert.AreEquivalent(new[] { "NEW", "NEW NEW" }, configBuilder.GetConfigs().Select(u => u.IndexName));
        }

        [Test]
        public void TestGetDocumet()
        {
            var config = new IndexConfig
            {
                ExcludedExtensions = "ABC",
                ExcludedPaths = "CDF",
                IncludedExtensions = "QQQ",
                IndexName = "AAA",
                MaxContentHighlightLength = 100,
                MonitorFolder = "BCD",
                MonitorFolderRealPath = "SSS",
                OpenIDEUriFormat = "DDDD",
                SaveIntervalSeconds = 22,
                Pk = Guid.NewGuid()
            };

            var document = ConfigIndexBuilder.GetDocumet(config);
            Assert.AreEqual(10, document.Fields.Count);

            var configConvertBack = document.GetObject<IndexConfig>();
            Assert.AreEqual(config.ToString(), configConvertBack.ToString());
        }
    }
}
