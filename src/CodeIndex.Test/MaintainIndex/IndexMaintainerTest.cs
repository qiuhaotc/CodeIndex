using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CodeIndex.Common;
using CodeIndex.MaintainIndex;
using Lucene.Net.Search;
using Microsoft.Extensions.Logging;
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
            Assert.That(maintainer.Status, Is.EqualTo(IndexStatus.Idle));

            await maintainer.InitializeIndex(false);
            Assert.That(maintainer.Status, Is.EqualTo(IndexStatus.Initialized));

            var codeDocuments = maintainer.IndexBuilder.CodeIndexPool.Search(new MatchAllDocsQuery(), int.MaxValue);
            Assert.That(codeDocuments.Length, Is.EqualTo(2));
            Assert.That(codeDocuments.Select(u => u.Get(nameof(CodeSource.Content))), Is.EquivalentTo(new[] { "ABCD ABCD" + Environment.NewLine + "ABCD", "ABCD EFGH" }));

            var hintDocuments = maintainer.IndexBuilder.HintIndexPool.Search(new MatchAllDocsQuery(), int.MaxValue);
            Assert.That(hintDocuments.Length, Is.EqualTo(2));
            Assert.That(hintDocuments.Select(u => u.Get(nameof(CodeWord.Word))), Is.EquivalentTo(new[] { "ABCD", "EFGH" }));
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
                Assert.That(Log.LogsContent.Contains("Add index for"), Is.True);
            }

            Log.ClearLog();

            using (var maintainer = new IndexMaintainer(indexConfig, Config, Log))
            {
                await maintainer.InitializeIndex(false);

                Assert.That(Log.LogsContent.Contains("Delete All Index"), Is.False, "Don't delete all indexes when not force rebuild");
                Assert.That(Log.LogsContent.Contains("Add index for"), Is.False, "Do not generate already up to date file");

                var codeDocuments = maintainer.IndexBuilder.CodeIndexPool.Search(new MatchAllDocsQuery(), int.MaxValue);
                Assert.That(codeDocuments.Select(u => u.Get(nameof(CodeSource.Content))), Is.EquivalentTo(new[] { "ABCD ABCD" + Environment.NewLine + "ABCD", "ABCD" }));

                var hintDocuments = maintainer.IndexBuilder.HintIndexPool.Search(new MatchAllDocsQuery(), int.MaxValue);
                Assert.That(hintDocuments.Select(u => u.Get(nameof(CodeWord.Word))), Is.EquivalentTo(new[] { "ABCD" }));
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
                Assert.That(codeDocuments.Select(u => u.Get(nameof(CodeSource.Content))), Is.EquivalentTo(new[] { "ABCD ABCD" + Environment.NewLine + "ABCD WOWOWO", "File3" }));

                var hintDocuments = maintainer.IndexBuilder.HintIndexPool.Search(new MatchAllDocsQuery(), int.MaxValue);
                Assert.That(hintDocuments.Select(u => u.Get(nameof(CodeWord.Word))), Is.EquivalentTo(new[] { "ABCD", "WOWOWO", "File3" }));
            }

            Log.ClearLog();

            using (var maintainer = new IndexMaintainer(indexConfig, Config, Log))
            {
                await maintainer.InitializeIndex(true);
                Assert.That(Log.LogsContent.Contains("Delete All Index"), Is.True, "Delete existing indexes when force rebuild");

                var codeDocuments = maintainer.IndexBuilder.CodeIndexPool.Search(new MatchAllDocsQuery(), int.MaxValue);
                Assert.That(codeDocuments.Select(u => u.Get(nameof(CodeSource.Content))), Is.EquivalentTo(new[] { "ABCD ABCD" + Environment.NewLine + "ABCD WOWOWO", "File3" }));

                var hintDocuments = maintainer.IndexBuilder.HintIndexPool.Search(new MatchAllDocsQuery(), int.MaxValue);
                Assert.That(hintDocuments.Select(u => u.Get(nameof(CodeWord.Word))), Is.EquivalentTo(new[] { "ABCD", "WOWOWO", "File3" }));
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
            Assert.That(codeDocuments.Select(u => u.Get(nameof(CodeSource.FilePath))), Is.EquivalentTo(new[] { fileName1, fileName2 }));
            Assert.That(codeDocuments.Select(u => u.Get(nameof(CodeSource.Content))), Is.EquivalentTo(new[] { "ABCD ABCD", "ABCD" }));

            resetEvent.WaitOne(20000);

            Assert.That(maintainer.Status, Is.EqualTo(IndexStatus.Monitoring));
            codeDocuments = maintainer.IndexBuilder.CodeIndexPool.Search(new MatchAllDocsQuery(), int.MaxValue);
            Assert.That(codeDocuments.Select(u => u.Get(nameof(CodeSource.FilePath))), Is.EquivalentTo(new[] { fileName1, fileName3 }));
            Assert.That(codeDocuments.Select(u => u.Get(nameof(CodeSource.Content))), Is.EquivalentTo(new[] { "ABCD ABCD NewContent", "Created" }));
        }

        protected new DummyLog<IndexMaintainerForTest> Log => log ??= new DummyLog<IndexMaintainerForTest>();
        DummyLog<IndexMaintainerForTest> log;

        public class IndexMaintainerForTest : IndexMaintainer
        {
            public IndexMaintainerForTest(IndexConfig indexConfig, CodeIndexConfiguration codeIndexConfiguration, ILogger<IndexMaintainerForTest> log, AutoResetEvent resetEvent) : base(indexConfig, codeIndexConfiguration, log)
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
