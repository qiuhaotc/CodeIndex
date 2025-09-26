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

            Assert.That(configBuilder.GetConfigs().First().IndexName, Is.EqualTo("ABC"));
        }

        [Test]
        public void TestAddIndexConfig()
        {
            using var configBuilder = new ConfigIndexBuilder(TempConfigDir);
            configBuilder.AddIndexConfig(new IndexConfig
            {
                IndexName = "ABC"
            });

            Assert.That(configBuilder.GetConfigs().First().IndexName, Is.EqualTo("ABC"));

            configBuilder.AddIndexConfig(new IndexConfig
            {
                IndexName = "EFG"
            });

            Assert.That(configBuilder.GetConfigs().Select(u => u.IndexName), Is.EquivalentTo(new[] { "ABC", "EFG" }));
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
            Assert.That(configBuilder.GetConfigs().Count(), Is.EqualTo(2));

            configBuilder.DeleteIndexConfig(config2.Pk);
            Assert.That(configBuilder.GetConfigs().Select(u => u.IndexName), Is.EquivalentTo(new[] { "ABC" }));

            configBuilder.DeleteIndexConfig(config1.Pk);
            Assert.That(configBuilder.GetConfigs().Count(), Is.EqualTo(0));
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
            Assert.That(configBuilder.GetConfigs().Count(), Is.EqualTo(2));

            config1.IndexName = "NEW";
            configBuilder.EditIndexConfig(config1);
            Assert.That(configBuilder.GetConfigs().Select(u => u.IndexName), Is.EquivalentTo(new[] { "NEW", "EFG" }));

            config2.IndexName = "NEW NEW";
            configBuilder.EditIndexConfig(config2);
            Assert.That(configBuilder.GetConfigs().Select(u => u.IndexName), Is.EquivalentTo(new[] { "NEW", "NEW NEW" }));
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

            var document = ConfigIndexBuilder.GetDocument(config);
            Assert.That(document.Fields.Count, Is.EqualTo(10));

            var configConvertBack = document.GetObject<IndexConfig>();
            Assert.That(configConvertBack.ToString(), Is.EqualTo(config.ToString()));
        }
    }
}
