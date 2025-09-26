using System;
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

            Assert.That(config.Pk, Is.Not.EqualTo(Guid.Empty));
            Assert.That(config.ExcludedExtensions, Is.EqualTo("A|B|C"));
            Assert.That(config.ExcludedPaths, Is.EqualTo("B|C|D"));
            Assert.That(config.IncludedExtensions, Is.EqualTo("E|F"));
            Assert.That(config.ExcludedExtensionsArray, Is.EquivalentTo(new[] { "A", "B", "C" }));
            Assert.That(config.ExcludedPathsArray, Is.EquivalentTo(new[] { "B", "C", "D" }));
            Assert.That(config.IncludedExtensionsArray, Is.EquivalentTo(new[] { "E", "F" }));
            Assert.That(config.IndexName, Is.EqualTo("ABC"));
            Assert.That(config.MaxContentHighlightLength, Is.EqualTo(100));
            Assert.That(config.MonitorFolder, Is.EqualTo("BCA"));
            Assert.That(config.MonitorFolderRealPath, Is.EqualTo("AAA"));
            Assert.That(config.OpenIDEUriFormat, Is.EqualTo("BBB"));
            Assert.That(config.SaveIntervalSeconds, Is.EqualTo(10));

            config.IncludedExtensions = null;
            Assert.That(config.IncludedExtensionsArray.Count(), Is.EqualTo(0));
        }
    }
}
