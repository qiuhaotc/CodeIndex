using System.IO;
using System.Linq;
using CodeIndex.Files;
using NUnit.Framework;

namespace CodeIndex.Test
{
    public class FilesFetcherTest : BaseTest
    {
        [Test]
        public void TestFetchFiles()
        {
            Directory.CreateDirectory(Path.Combine(TempDir, "SubDir"));
            Directory.CreateDirectory(Path.Combine(TempDir, "bin\\debug"));
            File.Create(Path.Combine(TempDir, "AAA.cs")).Close();
            File.Create(Path.Combine(TempDir, "SubDir", "ddd.txt")).Close();
            File.Create(Path.Combine(TempDir, "SubDir", "ddd.xml")).Close();
            File.Create(Path.Combine(TempDir, "bin\\debug", "ddd.txt")).Close();

            var files = FilesFetcher.FetchAllFiles(TempDir, new[] { ".xml" }, new[] { "BIN\\" }).ToArray();
            Assert.That(files.Length, Is.EqualTo(2));
            Assert.That(files.Select(u => u.Name), Is.EquivalentTo(new[] { "AAA.cs", "ddd.txt" }));

            files = FilesFetcher.FetchAllFiles(TempDir, new[] { ".xml" }, new[] { "BIN\\" }, "*.cs").ToArray();
            Assert.That(files.Length, Is.EqualTo(1));
            Assert.That(files.Select(u => u.Name), Is.EquivalentTo(new[] { "AAA.cs" }));
        }
    }
}
