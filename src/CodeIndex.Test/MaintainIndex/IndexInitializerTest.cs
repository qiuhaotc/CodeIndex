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
            var codeFilePath = Path.Combine(TempDir, "Code");
            Directory.CreateDirectory(codeFilePath);
            File.WriteAllText(Path.Combine(codeFilePath, "A.txt"), "I have an apple");

            Directory.CreateDirectory(Path.Combine(codeFilePath, "Sub"));
            File.WriteAllText(Path.Combine(codeFilePath, "Sub", "B.txt"), "I have a peach");

            var log = new DummyLog();
            var initializer = new IndexInitializer(log);
            initializer.InitializeIndex(codeFilePath, TempIndexDir, Array.Empty<string>(), Array.Empty<string>());
            StringAssert.Contains($"Create index {TempIndexDir}", log.LogsContent);

            var codeSources = CodeIndexBuilder.GetAllIndexedCodeSource(TempIndexDir);
            Assert.AreEqual(2, codeSources.Count);
            CollectionAssert.AreEquivalent(new[] { Path.Combine(codeFilePath, "A.txt"), Path.Combine(codeFilePath, "Sub", "B.txt") }, codeSources.Select(u => u.FilePath));

            File.WriteAllText(Path.Combine(codeFilePath, "C.txt"), "I have a pear");
            File.AppendAllText(Path.Combine(codeFilePath, "A.txt"), "Now I have two apples");
            File.Delete(Path.Combine(codeFilePath, "Sub", "B.txt"));

            initializer.InitializeIndex(codeFilePath, TempIndexDir, Array.Empty<string>(), Array.Empty<string>());

            StringAssert.Contains("Compare index different", log.LogsContent);
            StringAssert.Contains($"File {Path.Combine(codeFilePath, "A.txt")} modified", log.LogsContent);
            StringAssert.Contains($"File {Path.Combine(codeFilePath, "Sub", "B.txt")} deleted", log.LogsContent);
            StringAssert.Contains($"Found new file {Path.Combine(codeFilePath, "C.txt")}", log.LogsContent);

            codeSources = CodeIndexBuilder.GetAllIndexedCodeSource(TempIndexDir);
            Assert.AreEqual(2, codeSources.Count);
            CollectionAssert.AreEquivalent(new[] { Path.Combine(codeFilePath, "A.txt"), Path.Combine(codeFilePath, "C.txt") }, codeSources.Select(u => u.FilePath));
        }

        [Test]
        public void TestGetAllIndexedCodeSource()
        {
            var codeFilePath = Path.Combine(TempDir, "Code");
            Directory.CreateDirectory(codeFilePath);
            File.WriteAllText(Path.Combine(codeFilePath, "A.txt"), "I have an apple");
            File.WriteAllText(Path.Combine(codeFilePath, "B.txt"), "I have two apples");

            var initializer = new IndexInitializer(null);
            initializer.InitializeIndex(codeFilePath, TempIndexDir, Array.Empty<string>(), Array.Empty<string>());

            var codeSources = CodeIndexBuilder.GetAllIndexedCodeSource(TempIndexDir);
            Assert.AreEqual(2, codeSources.Count);
            CollectionAssert.AreEquivalent(new[] { Path.Combine(codeFilePath, "A.txt"), Path.Combine(codeFilePath, "B.txt") }, codeSources.Select(u => u.FilePath));
            CollectionAssert.AreEquivalent(new[] { new FileInfo(Path.Combine(codeFilePath, "A.txt")).LastWriteTimeUtc, new FileInfo(Path.Combine(codeFilePath, "B.txt")).LastWriteTimeUtc }, codeSources.Select(u => u.LastWriteTimeUtc));
        }
    }
}
