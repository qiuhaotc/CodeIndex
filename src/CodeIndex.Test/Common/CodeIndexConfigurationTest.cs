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
                ExcludedExtensions = ".cs|.dll|.EXE",
                ExcludedPaths = "AAA|ccc",
                IncludedExtensions = ".txt|.temp",
                IsInLinux = true,
                LocalUrl = "http://localhost:1234",
                LuceneIndex = "AAA/BBB",
                MaxContentHighlightLength = 3030,
                MonitorFolder = "DDD/EEE",
                MonitorFolderRealPath = "EEE",
                OpenIDEUriFormat = "ABC{SSS}",
                SaveIntervalSeconds = 123,
                MaximumResults = 234
            };

            Assert.Multiple(() =>
            {
                Assert.AreEqual(".cs|.dll|.EXE", config.ExcludedExtensions);
                Assert.AreEqual("AAA|ccc", config.ExcludedPaths);
                Assert.AreEqual(".txt|.temp", config.IncludedExtensions);
                Assert.AreEqual(true, config.IsInLinux);
                Assert.AreEqual("http://localhost:1234", config.LocalUrl);
                Assert.AreEqual("AAA/BBB", config.LuceneIndex);
                Assert.AreEqual(3030, config.MaxContentHighlightLength);
                Assert.AreEqual("DDD/EEE", config.MonitorFolder);
                Assert.AreEqual("EEE", config.MonitorFolderRealPath);
                Assert.AreEqual("ABC{SSS}", config.OpenIDEUriFormat);
                Assert.AreEqual(123, config.SaveIntervalSeconds);
                Assert.AreEqual(234, config.MaximumResults);
                CollectionAssert.AreEquivalent(new[] { ".cs", ".dll", ".EXE" }, config.ExcludedExtensionsArray);
                CollectionAssert.AreEquivalent(new[] { "AAA", "ccc" }, config.ExcludedPathsArray);
                CollectionAssert.AreEquivalent(new[] { ".txt", ".temp" }, config.IncludedExtensionsArray);
            });
        }
    }
}
