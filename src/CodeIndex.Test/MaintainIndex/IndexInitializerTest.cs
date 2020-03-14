using System;
using System.IO;
using System.Linq;
using CodeIndex.IndexBuilder;
using CodeIndex.MaintainIndex;
using NUnit.Framework;

namespace CodeIndex.Test
{
    public class IndexInitializerTest : BaseTest
    {
        [Test]
        public void TestInitializeIndex()
        {
            Directory.CreateDirectory(MonitorFolder);
            File.WriteAllText(Path.Combine(MonitorFolder, "A.txt"), "I have an apple");

            Directory.CreateDirectory(Path.Combine(MonitorFolder, "Sub"));
            File.WriteAllText(Path.Combine(MonitorFolder, "Sub", "B.txt"), "I have a peach");

            var log = new DummyLog();
            var initializer = new IndexInitializer(log);
            initializer.InitializeIndex(Config, Array.Empty<string>(), Array.Empty<string>(), out _);
            StringAssert.Contains($"Create index folder {Config.LuceneIndexForCode}", log.LogsContent);
            StringAssert.Contains($"Create index folder {Config.LuceneIndexForHint}", log.LogsContent);

            var codeSources = CodeIndexBuilder.GetAllIndexedCodeSource(Config.LuceneIndexForCode);
            Assert.AreEqual(2, codeSources.Count);
            CollectionAssert.AreEquivalent(new[] { Path.Combine(MonitorFolder, "A.txt"), Path.Combine(MonitorFolder, "Sub", "B.txt") }, codeSources.Select(u => u.FilePath));

            File.WriteAllText(Path.Combine(MonitorFolder, "C.txt"), "I have a pear");
            File.AppendAllText(Path.Combine(MonitorFolder, "A.txt"), "Now I have two apples");
            File.Delete(Path.Combine(MonitorFolder, "Sub", "B.txt"));

            initializer.InitializeIndex(Config, Array.Empty<string>(), Array.Empty<string>(), out _);

            StringAssert.Contains("Compare index difference", log.LogsContent);
            StringAssert.Contains($"File {Path.Combine(MonitorFolder, "A.txt")} modified", log.LogsContent);
            StringAssert.Contains($"File {Path.Combine(MonitorFolder, "Sub", "B.txt")} deleted", log.LogsContent);
            StringAssert.Contains($"Found new file {Path.Combine(MonitorFolder, "C.txt")}", log.LogsContent);

            codeSources = CodeIndexBuilder.GetAllIndexedCodeSource(Config.LuceneIndexForCode);
            Assert.AreEqual(2, codeSources.Count);
            CollectionAssert.AreEquivalent(new[] { Path.Combine(MonitorFolder, "A.txt"), Path.Combine(MonitorFolder, "C.txt") }, codeSources.Select(u => u.FilePath));
        }

        [Test]
        public void TestGetAllIndexedCodeSource()
        {
            Directory.CreateDirectory(MonitorFolder);
            File.WriteAllText(Path.Combine(MonitorFolder, "A.txt"), "I have an apple");
            File.WriteAllText(Path.Combine(MonitorFolder, "B.txt"), "I have two apples");

            var initializer = new IndexInitializer(null);
            initializer.InitializeIndex(Config, Array.Empty<string>(), Array.Empty<string>(), out _);

            var codeSources = CodeIndexBuilder.GetAllIndexedCodeSource(Config.LuceneIndexForCode);
            Assert.AreEqual(2, codeSources.Count);
            CollectionAssert.AreEquivalent(new[] { Path.Combine(MonitorFolder, "A.txt"), Path.Combine(MonitorFolder, "B.txt") }, codeSources.Select(u => u.FilePath));
            CollectionAssert.AreEquivalent(new[] { new FileInfo(Path.Combine(MonitorFolder, "A.txt")).LastWriteTimeUtc, new FileInfo(Path.Combine(MonitorFolder, "B.txt")).LastWriteTimeUtc }, codeSources.Select(u => u.LastWriteTimeUtc));
        }
    }
}
