using CodeIndex.Common;
using NUnit.Framework;
using System;

namespace CodeIndex.Test
{
    class IndexConfigTest
    {
        [Test]
        public void TestConstructor()
        {
            var pk = Guid.NewGuid();
            var config = new IndexConfig
            {
                ExcludedExtensions = new[] { "A" },
                ExcludedPaths = new[] { "B" },
                IncludedExtensions = new[] { "C" },
                IndexCreatedDate = new DateTime(2020, 1, 1),
                IndexLastUpdatedDate = new DateTime(2020, 1, 2),
                IndexName = "ABC",
                MaxContentHighlightLength = 100,
                MonitorFolder = "BCA",
                MonitorFolderRealPath = "AAA",
                OpenIDEUriFormat = "BBB",
                Pk = pk,
                SaveIntervalSeconds = 10
            };

            CollectionAssert.AreEquivalent(config.ExcludedExtensions, new[] { "A" });
            CollectionAssert.AreEquivalent(config.ExcludedPaths, new[] { "B" });
            CollectionAssert.AreEquivalent(config.IncludedExtensions, new[] { "C" });
            Assert.AreEqual(new DateTime(2020, 1, 1), config.IndexCreatedDate);
            Assert.AreEqual(new DateTime(2020, 1, 2), config.IndexLastUpdatedDate);
            Assert.AreEqual("ABC", config.IndexName);
            Assert.AreEqual(100, config.MaxContentHighlightLength);
            Assert.AreEqual("BCA", config.MonitorFolder);
            Assert.AreEqual("AAA", config.MonitorFolderRealPath);
            Assert.AreEqual("BBB", config.OpenIDEUriFormat);
            Assert.AreEqual(pk, config.Pk);
            Assert.AreEqual(10, config.SaveIntervalSeconds);
        }
    }
}