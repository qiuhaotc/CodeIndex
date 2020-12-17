using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CodeIndex.Common;
using CodeIndex.MaintainIndex;
using Lucene.Net.Search;
using NUnit.Framework;

namespace CodeIndex.Test
{
    public class IndexMaintainerTest : BaseTest
    {
        [Test]
        public async Task TestInitializeIndex()
        {
            var indexConfig = new IndexConfig
            {
                ExcludedExtensions = ".dll",
                SaveIntervalSeconds = 1,
                IndexName = "ABC",
                MonitorFolder = MonitorFolder
            };

            var fileName1 = Path.Combine(MonitorFolder, "A.txt");
            var fileName2 = Path.Combine(MonitorFolder, "B.txt");
            File.AppendAllText(fileName1, "ABCD ABCD" + Environment.NewLine + "ABCD");
            File.AppendAllText(fileName2, "ABCD EFGH");

            using var maintainer = new IndexMaintainer(indexConfig, Config, Log);
            Assert.AreEqual(IndexStatus.Idle, maintainer.Status);

            await maintainer.InitializeIndex(false);
            Assert.AreEqual(IndexStatus.Initialized, maintainer.Status);

            var codeDocuments = maintainer.IndexBuilder.CodeIndexPool.Search(new MatchAllDocsQuery(), int.MaxValue);
            Assert.AreEqual(2, codeDocuments.Length);
            CollectionAssert.AreEquivalent(new[] { "ABCD ABCD" + Environment.NewLine + "ABCD", "ABCD EFGH" }, codeDocuments.Select(u => u.Get(nameof(CodeSource.Content))));

            var hintDocuments = maintainer.IndexBuilder.HintIndexPool.Search(new MatchAllDocsQuery(), int.MaxValue);
            Assert.AreEqual(2, hintDocuments.Length);
            CollectionAssert.AreEquivalent(new[] { "ABCD", "EFGH" }, hintDocuments.Select(u => u.Get(nameof(CodeWord.Word))));
        }

        [Test]
        public async Task TestInitializeIndex_ReInitialize()
        {
            var indexConfig = new IndexConfig
            {
                ExcludedExtensions = ".dll",
                SaveIntervalSeconds = 1,
                IndexName = "ABC",
                MonitorFolder = MonitorFolder
            };

            var fileName1 = Path.Combine(MonitorFolder, "A.txt");
            var fileName2 = Path.Combine(MonitorFolder, "B.txt");
            var fileNameExcluded = Path.Combine(MonitorFolder, "B.dll");
            File.AppendAllText(fileName1, "ABCD ABCD" + Environment.NewLine + "ABCD");
            File.AppendAllText(fileName2, "ABCD");
            File.AppendAllText(fileNameExcluded, "a excluded file");

            using (var maintainer = new IndexMaintainer(indexConfig, Config, Log))
            {
                await maintainer.InitializeIndex(false);
                Assert.IsTrue(Log.LogsContent.Contains("Add index for"));
            }

            Log.ClearLog();

            using (var maintainer = new IndexMaintainer(indexConfig, Config, Log))
            {
                await maintainer.InitializeIndex(false);

                Assert.IsFalse(Log.LogsContent.Contains("Delete All Index"), "Don't delete all indexes when not force rebuild");
                Assert.IsFalse(Log.LogsContent.Contains("Add index for"), "Do not generate already up to date file");

                var codeDocuments = maintainer.IndexBuilder.CodeIndexPool.Search(new MatchAllDocsQuery(), int.MaxValue);
                CollectionAssert.AreEquivalent(new[] { "ABCD ABCD" + Environment.NewLine + "ABCD", "ABCD" }, codeDocuments.Select(u => u.Get(nameof(CodeSource.Content))));

                var hintDocuments = maintainer.IndexBuilder.HintIndexPool.Search(new MatchAllDocsQuery(), int.MaxValue);
                CollectionAssert.AreEquivalent(new[] { "ABCD" }, hintDocuments.Select(u => u.Get(nameof(CodeWord.Word))));
            }

            Log.ClearLog();
            var fileName3 = Path.Combine(MonitorFolder, "C.txt");
            File.AppendAllText(fileName3, "File3"); // New
            File.Delete(fileName2); // Deleted
            File.AppendAllText(fileName1, " WOWOWO"); // Edit

            using (var maintainer = new IndexMaintainer(indexConfig, Config, Log))
            {
                await maintainer.InitializeIndex(false);

                var codeDocuments = maintainer.IndexBuilder.CodeIndexPool.Search(new MatchAllDocsQuery(), int.MaxValue);
                CollectionAssert.AreEquivalent(new[] { "ABCD ABCD" + Environment.NewLine + "ABCD WOWOWO", "File3" }, codeDocuments.Select(u => u.Get(nameof(CodeSource.Content))));

                var hintDocuments = maintainer.IndexBuilder.HintIndexPool.Search(new MatchAllDocsQuery(), int.MaxValue);
                CollectionAssert.AreEquivalent(new[] { "ABCD", "WOWOWO", "File3" }, hintDocuments.Select(u => u.Get(nameof(CodeWord.Word))));
            }

            Log.ClearLog();

            using (var maintainer = new IndexMaintainer(indexConfig, Config, Log))
            {
                await maintainer.InitializeIndex(true);
                Assert.IsTrue(Log.LogsContent.Contains("Delete All Index"), "Delete existing indexes when force rebuild");

                var codeDocuments = maintainer.IndexBuilder.CodeIndexPool.Search(new MatchAllDocsQuery(), int.MaxValue);
                CollectionAssert.AreEquivalent(new[] { "ABCD ABCD" + Environment.NewLine + "ABCD WOWOWO", "File3" }, codeDocuments.Select(u => u.Get(nameof(CodeSource.Content))));

                var hintDocuments = maintainer.IndexBuilder.HintIndexPool.Search(new MatchAllDocsQuery(), int.MaxValue);
                CollectionAssert.AreEquivalent(new[] { "ABCD", "WOWOWO", "File3" }, hintDocuments.Select(u => u.Get(nameof(CodeWord.Word))));
            }
        }

        [Test]
        public async Task TestMaintainIndexes()
        {
            var indexConfig = new IndexConfig
            {
                ExcludedExtensions = ".dll",
                SaveIntervalSeconds = 1,
                IndexName = "ABC",
                MonitorFolder = MonitorFolder
            };

            var fileName1 = Path.Combine(MonitorFolder, "A.txt");
            var fileName2 = Path.Combine(MonitorFolder, "B.txt");
            var fileName3 = Path.Combine(MonitorFolder, "C.txt");
            File.AppendAllText(fileName1, "ABCD ABCD");
            File.AppendAllText(fileName2, "ABCD");

            var resetEvent = new AutoResetEvent(false);
            using var maintainer = new IndexMaintainerForTest(indexConfig, Config, Log, resetEvent);
            await maintainer.InitializeIndex(false);
            _ = maintainer.MaintainIndexes();
            File.AppendAllText(fileName1, " NewContent");
            File.Delete(fileName2);
            File.AppendAllText(fileName3, "Created");

            var codeDocuments = maintainer.IndexBuilder.CodeIndexPool.Search(new MatchAllDocsQuery(), int.MaxValue);
            CollectionAssert.AreEquivalent(new[] { fileName1, fileName2 }, codeDocuments.Select(u => u.Get(nameof(CodeSource.FilePath))));
            CollectionAssert.AreEquivalent(new[] { "ABCD ABCD", "ABCD" }, codeDocuments.Select(u => u.Get(nameof(CodeSource.Content))));

            resetEvent.WaitOne(20000);

            Assert.AreEqual(IndexStatus.Monitoring, maintainer.Status);
            codeDocuments = maintainer.IndexBuilder.CodeIndexPool.Search(new MatchAllDocsQuery(), int.MaxValue);
            CollectionAssert.AreEquivalent(new[] { fileName1, fileName3 }, codeDocuments.Select(u => u.Get(nameof(CodeSource.FilePath))));
            CollectionAssert.AreEquivalent(new[] { "ABCD ABCD NewContent", "Created" }, codeDocuments.Select(u => u.Get(nameof(CodeSource.Content))));
        }

        class IndexMaintainerForTest : IndexMaintainer
        {
            public IndexMaintainerForTest(IndexConfig indexConfig, CodeIndexConfiguration codeIndexConfiguration, ILog log, AutoResetEvent resetEvent) : base(indexConfig, codeIndexConfiguration, log)
            {
                ResetEvent = resetEvent;
            }

            public AutoResetEvent ResetEvent { get; }

            protected override int FetchIntervalSeconds => 1;

            protected override void TriggerCommitFinished()
            {
                ResetEvent.Set();
            }
        }
    }
}
