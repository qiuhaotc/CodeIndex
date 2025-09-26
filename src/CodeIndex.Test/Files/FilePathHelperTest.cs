using CodeIndex.Files;
using NUnit.Framework;

namespace CodeIndex.Test
{
    public class FilePathHelperTest
    {
        [Test]
        public void TestGetPaths()
        {
            var results = FilePathHelper.GetPaths(new[] { "\\BIN\\1.txt", "/home/etc" }, true);
            Assert.That(results, Is.EquivalentTo(new[] { "/BIN/1.TXT", "/HOME/ETC" }));

            results = FilePathHelper.GetPaths(new[] { "\\BIN\\1.txt", "/home/etc" }, false);
            Assert.That(results, Is.EquivalentTo(new[] { "\\BIN\\1.TXT", "\\HOME\\ETC" }));

            Assert.That(FilePathHelper.GetPaths(null, true), Is.Null);
            Assert.That(FilePathHelper.GetPaths(null, false), Is.Null);
        }
    }
}
