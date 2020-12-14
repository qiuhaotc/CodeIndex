using System;
using System.IO;
using System.Linq;
using System.Threading;
using CodeIndex.Common;
using CodeIndex.IndexBuilder;
using Lucene.Net.Index;
using Lucene.Net.Search;
using NUnit.Framework;

namespace CodeIndex.Test
{
    public class CodeIndexBuilderTest : BaseTest
    {
        [Test]
        public void TestInitIndexFolderIfNeeded()
        {
            using var indexBuilder = new CodeIndexBuilder("ABC", new LucenePoolLight(TempCodeIndexDir), new LucenePoolLight(TempHintIndexDir), Log);
            Assert.IsFalse(Directory.Exists(TempCodeIndexDir));
            Assert.IsFalse(Directory.Exists(TempHintIndexDir));

            indexBuilder.InitIndexFolderIfNeeded();
            Assert.IsTrue(Directory.Exists(TempCodeIndexDir));
            Assert.IsTrue(Directory.Exists(TempHintIndexDir));
        }

        [Test]
        public void TestBuildIndexByBatch()
        {
            using var indexBuilder = new CodeIndexBuilder("ABC", new LucenePoolLight(TempCodeIndexDir), new LucenePoolLight(TempHintIndexDir), Log);
            var fileName1 = Path.Combine(MonitorFolder, "A.txt");
            var fileName2 = Path.Combine(MonitorFolder, "B.txt");
            File.AppendAllText(fileName1, "ABCD ABCD" + Environment.NewLine + "ABCD");
            File.AppendAllText(fileName2, "ABCD EFGH");

            var failedFiles = indexBuilder.BuildIndexByBatch(new[] { new FileInfo(fileName1), new FileInfo(fileName2) }, true, true, true, CancellationToken.None);
            CollectionAssert.IsEmpty(failedFiles);

            var results = indexBuilder.CodeIndexPool.SearchCode(Generator.GetQueryFromStr("ABCD"));

            Assert.AreEqual(2, results.Length);
            Assert.AreEqual("A.txt", results[0].FileName);
            Assert.AreEqual("txt", results[0].FileExtension);
            Assert.AreEqual(fileName1, results[0].FilePath);
            Assert.AreEqual("ABCD ABCD" + Environment.NewLine + "ABCD", results[0].Content);
            Assert.GreaterOrEqual(DateTime.UtcNow, results[0].IndexDate);
            Assert.AreEqual("B.txt", results[1].FileName);
            Assert.AreEqual("txt", results[1].FileExtension);
            Assert.AreEqual(fileName2, results[1].FilePath);
            Assert.AreEqual("ABCD EFGH", results[1].Content);
            Assert.GreaterOrEqual(DateTime.UtcNow, results[1].IndexDate);
            Assert.AreEqual(1, indexBuilder.CodeIndexPool.SearchCode(Generator.GetQueryFromStr("EFGH")).Length);
            Assert.AreEqual(1, indexBuilder.HintIndexPool.SearchWord(new TermQuery(new Term(nameof(CodeWord.Word), "ABCD"))).Length);
            Assert.AreEqual(1, indexBuilder.HintIndexPool.SearchWord(new TermQuery(new Term(nameof(CodeWord.Word), "EFGH"))).Length);
        }

        [Test]
        public void TestBuildIndexByBatch_FailedFiles()
        {
            using var indexBuilder = new CodeIndexBuilder("ABC", new LucenePoolLight(TempCodeIndexDir), new LucenePoolLight(TempHintIndexDir), Log);
            var fileName1 = Path.Combine(MonitorFolder, "A.txt");
            var fileName2 = Path.Combine(MonitorFolder, "B.txt");
            var file = File.Create(fileName1);
            File.AppendAllText(fileName2, "ABCD ABCD");
            try
            {
                var failedFiles = indexBuilder.BuildIndexByBatch(new[] { new FileInfo(fileName1), new FileInfo(fileName2) }, true, true, true, CancellationToken.None);
                CollectionAssert.AreEquivalent(new[] { fileName1 }, failedFiles.Select(u => u.FullName));
            }
            finally
            {
                file.Dispose();
            }
        }

        [Test]
        public void TestCreateIndex()
        {
            using var indexBuilder = new CodeIndexBuilder("ABC", new LucenePoolLight(TempCodeIndexDir), new LucenePoolLight(TempHintIndexDir), Log);

            var fileName = Path.Combine(MonitorFolder, "A.txt");
            File.AppendAllText(fileName, "ABCD EEEE");
            Assert.IsTrue(indexBuilder.CreateIndex(new FileInfo(fileName)));

            var results = indexBuilder.CodeIndexPool.SearchCode(new MatchAllDocsQuery());
            var words = indexBuilder.HintIndexPool.SearchWord(new MatchAllDocsQuery());
            Assert.AreEqual(1, results.Length);
            Assert.AreEqual(2, words.Length);
            Assert.AreEqual("A.txt", results[0].FileName);
            Assert.AreEqual("txt", results[0].FileExtension);
            Assert.AreEqual(fileName, results[0].FilePath);
            Assert.AreEqual("ABCD EEEE", results[0].Content);
            Assert.GreaterOrEqual(DateTime.UtcNow, results[0].IndexDate);
            CollectionAssert.AreEquivalent(new[] { "ABCD", "EEEE" }, words.Select(u => u.Word));
        }

        [Test]
        public void TestRenameFolderIndexes()
        {
            using var indexBuilder = new CodeIndexBuilder("ABC", new LucenePoolLight(TempCodeIndexDir), new LucenePoolLight(TempHintIndexDir), Log);

            var oldPath = Path.Combine(MonitorFolder, "ABC");
            Directory.CreateDirectory(oldPath);
            var newPath = Path.Combine(MonitorFolder, "EFG");
            var oldFileName = Path.Combine(oldPath, "A.txt");
            var newFileName = Path.Combine(newPath, "A.txt");
            File.AppendAllText(oldFileName, "ABCD EEEE");
            Assert.IsTrue(indexBuilder.CreateIndex(new FileInfo(oldFileName)));
            Assert.AreEqual(oldFileName, indexBuilder.CodeIndexPool.SearchCode(new MatchAllDocsQuery()).First().FilePath);

            Directory.Move(oldPath, newPath);
            Assert.IsTrue(indexBuilder.RenameFolderIndexes(oldPath, newPath, CancellationToken.None));
            Assert.AreEqual(newFileName, indexBuilder.CodeIndexPool.SearchCode(new MatchAllDocsQuery()).First().FilePath);

            var results = indexBuilder.CodeIndexPool.SearchCode(new PrefixQuery(indexBuilder.GetNoneTokenizeFieldTerm(nameof(CodeSource.FilePath), newPath)));
            Assert.AreEqual(1, results.Length);
            Assert.AreEqual(newFileName, results[0].FilePath);
            Assert.AreEqual("ABCD EEEE", results[0].Content);
        }

        [Test]
        public void TestRenameFileIndex()
        {
            using var indexBuilder = new CodeIndexBuilder("ABC", new LucenePoolLight(TempCodeIndexDir), new LucenePoolLight(TempHintIndexDir), Log);

            var oldFileName = Path.Combine(MonitorFolder, "A.txt");
            var newFileName = Path.Combine(MonitorFolder, "B.txt");
            File.AppendAllText(oldFileName, "ABCD EEEE");
            Assert.IsTrue(indexBuilder.CreateIndex(new FileInfo(oldFileName)));
            Assert.AreEqual(oldFileName, indexBuilder.CodeIndexPool.SearchCode(new MatchAllDocsQuery()).First().FilePath);

            Directory.Move(oldFileName, newFileName);
            Assert.IsTrue(indexBuilder.RenameFileIndex(oldFileName, newFileName));
            Assert.AreEqual(newFileName, indexBuilder.CodeIndexPool.SearchCode(new MatchAllDocsQuery()).First().FilePath);

            var results = indexBuilder.CodeIndexPool.SearchCode(new TermQuery(indexBuilder.GetNoneTokenizeFieldTerm(nameof(CodeSource.FilePath), newFileName)));
            Assert.AreEqual(1, results.Length);
            Assert.AreEqual(newFileName, results[0].FilePath);
            Assert.AreEqual("ABCD EEEE", results[0].Content);
        }

        [Test]
        public void TestUpdateIndex()
        {
            using var indexBuilder = new CodeIndexBuilder("ABC", new LucenePoolLight(TempCodeIndexDir), new LucenePoolLight(TempHintIndexDir), Log);

            var fileName = Path.Combine(MonitorFolder, "A.txt");
            File.AppendAllText(fileName, "ABCD EEEE");
            Assert.IsTrue(indexBuilder.CreateIndex(new FileInfo(fileName)));
            Assert.AreEqual("ABCD EEEE", indexBuilder.CodeIndexPool.SearchCode(new MatchAllDocsQuery()).First().Content);

            File.AppendAllText(fileName, " NEW");
            Assert.IsTrue(indexBuilder.UpdateIndex(new FileInfo(fileName), CancellationToken.None));
            Assert.AreEqual("ABCD EEEE NEW", indexBuilder.CodeIndexPool.SearchCode(new MatchAllDocsQuery()).First().Content);
        }

        [Test]
        public void TestDeleteIndex()
        {
            using var indexBuilder = new CodeIndexBuilder("ABC", new LucenePoolLight(TempCodeIndexDir), new LucenePoolLight(TempHintIndexDir), Log);

            File.AppendAllText(Path.Combine(MonitorFolder, "A.txt"), "ABCD EEEE");
            File.AppendAllText(Path.Combine(MonitorFolder, "B.txt"), "ABCD DDDD");
            Assert.IsTrue(indexBuilder.CreateIndex(new FileInfo(Path.Combine(MonitorFolder, "A.txt"))));
            Assert.IsTrue(indexBuilder.CreateIndex(new FileInfo(Path.Combine(MonitorFolder, "B.txt"))));
            Assert.AreEqual(2, indexBuilder.CodeIndexPool.SearchCode(new MatchAllDocsQuery()).Length);

            Assert.IsTrue(indexBuilder.DeleteIndex(Path.Combine(MonitorFolder, "A.txt")));
            Assert.AreEqual(1, indexBuilder.CodeIndexPool.SearchCode(new MatchAllDocsQuery()).Length);
        }

        [Test]
        public void TestGetNoneTokenizeFieldTerm()
        {
            using var indexBuilder = new CodeIndexBuilder("ABC", new LucenePoolLight(TempCodeIndexDir), new LucenePoolLight(TempHintIndexDir), Log);

            var term = indexBuilder.GetNoneTokenizeFieldTerm("ABC", "Value");
            Assert.AreEqual("ABC" + Constants.NoneTokenizeFieldSuffix, term.Field);
            Assert.AreEqual("Value", term.Text());
        }

        [Test]
        public void TestCommit()
        {
            using var indexBuilder = new CodeIndexBuilder("ABC", new LucenePoolLight(TempCodeIndexDir), new LucenePoolLight(TempHintIndexDir), Log);
            File.AppendAllText(Path.Combine(MonitorFolder, "A.txt"), "ABCD EEEE");
            Assert.IsTrue(indexBuilder.CreateIndex(new FileInfo(Path.Combine(MonitorFolder, "A.txt"))));
            Assert.IsFalse(IndexBuilderHelper.IndexExists(TempCodeIndexDir));

            indexBuilder.Commit();
            Assert.IsTrue(IndexBuilderHelper.IndexExists(TempCodeIndexDir));

            using var dir = Lucene.Net.Store.FSDirectory.Open(TempCodeIndexDir);
            using var reader = DirectoryReader.Open(dir);
            var searcher = new IndexSearcher(reader);
            Assert.AreEqual(1, searcher.Search(new MatchAllDocsQuery(), 1).TotalHits);
        }

        [Test]
        public void TestDeleteAllIndex()
        {
            using var indexBuilder = new CodeIndexBuilder("ABC", new LucenePoolLight(TempCodeIndexDir), new LucenePoolLight(TempHintIndexDir), Log);
            Assert.IsFalse(Directory.Exists(TempCodeIndexDir));
            Assert.IsFalse(Directory.Exists(TempHintIndexDir));

            var fileName1 = Path.Combine(MonitorFolder, "A.txt");
            var fileName2 = Path.Combine(MonitorFolder, "B.txt");
            File.AppendAllText(fileName1, "ABCD ABCD");
            File.AppendAllText(fileName2, "ABCD EFGH");

            var failedFiles = indexBuilder.BuildIndexByBatch(new[] { new FileInfo(fileName1), new FileInfo(fileName2) }, true, true, true, CancellationToken.None);
            CollectionAssert.IsEmpty(failedFiles);
            Assert.AreEqual(2, indexBuilder.CodeIndexPool.SearchCode(new MatchAllDocsQuery()).Length);
            Assert.AreEqual(2, indexBuilder.HintIndexPool.SearchWord(new MatchAllDocsQuery()).Length);

            indexBuilder.DeleteAllIndex();
            Assert.AreEqual(0, indexBuilder.CodeIndexPool.SearchCode(new MatchAllDocsQuery()).Length);
            Assert.AreEqual(0, indexBuilder.HintIndexPool.SearchWord(new MatchAllDocsQuery()).Length);
        }
    }
}
