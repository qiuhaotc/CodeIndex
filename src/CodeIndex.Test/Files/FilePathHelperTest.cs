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
            CollectionAssert.AreEquivalent(new[] { "/BIN/1.TXT", "/HOME/ETC" }, results);

            results = FilePathHelper.GetPaths(new[] { "\\BIN\\1.txt", "/home/etc" }, false);
            CollectionAssert.AreEquivalent(new[] { "\\BIN\\1.TXT", "\\HOME\\ETC" }, results);

            Assert.IsNull(FilePathHelper.GetPaths(null, true));
            Assert.IsNull(FilePathHelper.GetPaths(null, false));
        }
    }
}
