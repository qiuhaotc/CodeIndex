using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using CodeIndex.Common;
using CodeIndex.IndexBuilder;
using CodeIndex.MaintainIndex;
using CodeIndex.Search;
using Lucene.Net.Search;
using NUnit.Framework;

namespace CodeIndex.Test
{
    [ExcludeFromCodeCoverage]
    public class CodeFilesIndexMaintainerTest : BaseTest
    {
        [Test]
        public void TestMaintainerIndex()
        {
            Config.ExcludedExtensions = ".dll";
            Config.SaveIntervalSeconds = 1;

            var waitMS = 1500;
            Directory.CreateDirectory(MonitorFolder);
            Directory.CreateDirectory(Path.Combine(MonitorFolder, "FolderA"));
            Directory.CreateDirectory(Path.Combine(MonitorFolder, "FolderB"));

            var fileAPath = Path.Combine(MonitorFolder, "FolderA", "AAA.cs");
            File.Create(fileAPath).Close();
            File.AppendAllText(fileAPath, "12345");

            var fileBPath = Path.Combine(MonitorFolder, "FolderB", "BBB.xml");
            File.Create(fileBPath).Close();
            File.AppendAllText(fileBPath, "this is a content for test, that's it\r\na new line;");

            var fileCPath = Path.Combine(MonitorFolder, "CCC.xml");
            File.Create(fileCPath).Close();
            File.AppendAllText(fileCPath, "this is a content for test");

            var fileDPath = Path.Combine(MonitorFolder, "DDD.txt");

            CodeIndexBuilder.BuildIndex(Config, true, true, true,
                new[]
                {
                    CodeSource.GetCodeSource(new FileInfo(fileAPath), "12345"),
                    CodeSource.GetCodeSource(new FileInfo(fileBPath), "this is a content for test, that's it\r\na new line;"),
                    CodeSource.GetCodeSource(new FileInfo(fileCPath), "this is a content for test")
                });

            LucenePool.SaveResultsAndClearLucenePool(Config.LuceneIndexForCode);

            var codeSources = CodeIndexSearcher.SearchCode(Config.LuceneIndexForCode, new MatchAllDocsQuery(), 100);
            CollectionAssert.AreEquivalent(new[] { "AAA.cs", "BBB.xml", "CCC.xml" }, codeSources.Select(u => u.FileName));

            using var maintainer = new CodeFilesIndexMaintainer(Config, new DummyLog());
            maintainer.StartWatch();
            maintainer.SetInitalizeFinishedToTrue();

            File.AppendAllText(fileAPath, "56789"); // Changed
            File.Delete(fileBPath); // Deleted
            File.Move(fileCPath, Path.Combine(MonitorFolder, "NewCCC.xml")); // Rename
            File.Create(fileDPath).Close(); // Created

            Thread.Sleep(waitMS); // wait task finish saving

            codeSources = CodeIndexSearcher.SearchCode(Config.LuceneIndexForCode, new MatchAllDocsQuery(), 100);

            Assert.Multiple(() =>
            {
                CollectionAssert.AreEquivalent(new[] { "AAA.cs", "NewCCC.xml", "DDD.txt" }, codeSources.Select(u => u.FileName));
                CollectionAssert.AreEquivalent(new[] { "1234556789", "this is a content for test", string.Empty }, codeSources.Select(u => u.Content));
                CollectionAssert.AreEquivalent(new[] { fileAPath, Path.Combine(MonitorFolder, "NewCCC.xml"), fileDPath }, codeSources.Select(u => u.FilePath));
            });

            maintainer.Dispose();
        }

        [Test]
        public void TestMaintainerIndex_RetryFailed()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Assert.Pass();
                return;
            }

            var waitMS = 1500;
            Directory.CreateDirectory(MonitorFolder);

            var fileAPath = Path.Combine(MonitorFolder, "AAA.cs");
            File.Create(fileAPath).Close();
            File.AppendAllText(fileAPath, "12345");

            Config.SaveIntervalSeconds = 1;
            Config.ExcludedExtensions = ".dll";
            using var maintainer = new CodeFilesIndexMaintainerForTest(Config, new DummyLog());
            maintainer.StartWatch();
            maintainer.SetInitalizeFinishedToTrue();

            maintainer.PendingRetryCodeSources.Enqueue(new PendingRetrySource()
            {
                ChangesType = WatcherChangeTypes.Created,
                FilePath = fileAPath,
                LastRetryUTCDate = DateTime.Now.AddDays(-1)
            });

            var retryTime = 3;
            var codeSources = Array.Empty<CodeSource>();

            while (retryTime > 0)
            {
                Thread.Sleep(waitMS); // wait task finish saving
                retryTime--;
                codeSources = CodeIndexSearcher.SearchCode(Config.LuceneIndexForCode, new MatchAllDocsQuery(), 100);

                if (codeSources.Length > 0)
                {
                    break;
                }
            }

            Assert.AreEqual(1, codeSources.Length);
            Assert.AreEqual("AAA.cs", codeSources[0].FileName);
        }

        class CodeFilesIndexMaintainerForTest : CodeFilesIndexMaintainer
        {
            public CodeFilesIndexMaintainerForTest(CodeIndexConfiguration config, ILog log) : base(config, log)
            {
            }

            public ConcurrentQueue<PendingRetrySource> PendingRetryCodeSources => pendingRetryCodeSources;

            protected override int SleepMilliseconds => 100;
        }
    }
}
