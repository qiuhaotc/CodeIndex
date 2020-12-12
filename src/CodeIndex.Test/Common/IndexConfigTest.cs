using System.Linq;
using CodeIndex.Common;
using NUnit.Framework;

namespace CodeIndex.Test
{
    class IndexConfigTest
    {
        [Test]
        public void TestConstructor()
        {
            var config = new IndexConfig
            {
                ExcludedExtensions = "A|B|C",
                ExcludedPaths = "B|C|D",
                IncludedExtensions = "E|F",
                IndexName = "ABC",
                MaxContentHighlightLength = 100,
                MonitorFolder = "BCA",
                MonitorFolderRealPath = "AAA",
                OpenIDEUriFormat = "BBB",
                SaveIntervalSeconds = 10
            };

            Assert.AreEqual("A|B|C", config.ExcludedExtensions);
            Assert.AreEqual("B|C|D", config.ExcludedPaths);
            Assert.AreEqual("E|F", config.IncludedExtensions);
            CollectionAssert.AreEquivalent(config.ExcludedExtensionsArray, new[] { "A", "B", "C" });
            CollectionAssert.AreEquivalent(config.ExcludedPathsArray, new[] { "B", "C", "D" });
            CollectionAssert.AreEquivalent(config.IncludedExtensionsArray, new[] { "E", "F" });
            Assert.AreEqual("ABC", config.IndexName);
            Assert.AreEqual(100, config.MaxContentHighlightLength);
            Assert.AreEqual("BCA", config.MonitorFolder);
            Assert.AreEqual("AAA", config.MonitorFolderRealPath);
            Assert.AreEqual("BBB", config.OpenIDEUriFormat);
            Assert.AreEqual(10, config.SaveIntervalSeconds);

            config.IncludedExtensions = null;
            Assert.AreEqual(0, config.IncludedExtensionsArray.Count());
        }
    }
}
