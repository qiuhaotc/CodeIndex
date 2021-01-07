using System;
using System.IO;
using System.Linq;
using System.Threading;
using CodeIndex.Common;
using CodeIndex.IndexBuilder;
using CodeIndex.MaintainIndex;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers.Classic;
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

            var failedFiles = indexBuilder.BuildIndexByBatch(new[] { new FileInfo(fileName1), new FileInfo(fileName2) }, true, true, true, CancellationToken.None, true);
            CollectionAssert.IsEmpty(failedFiles);

            var results = indexBuilder.CodeIndexPool.SearchCode(Generator.GetQueryFromStr("ABCD", false));

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
            Assert.AreEqual(1, indexBuilder.CodeIndexPool.SearchCode(Generator.GetQueryFromStr("EFGH", false)).Length);
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
                var failedFiles = indexBuilder.BuildIndexByBatch(new[] { new FileInfo(fileName1), new FileInfo(fileName2) }, true, true, true, CancellationToken.None, true);
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
            Assert.AreEqual(IndexBuildResults.Successful, indexBuilder.CreateIndex(new FileInfo(fileName)));

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

            Assert.AreEqual(IndexBuildResults.Successful, indexBuilder.CreateIndex(new FileInfo(fileName)));
            results = indexBuilder.CodeIndexPool.SearchCode(new MatchAllDocsQuery());
            words = indexBuilder.HintIndexPool.SearchWord(new MatchAllDocsQuery());
            Assert.AreEqual(1, results.Length, "No Duplicate Index Created");
            Assert.AreEqual(2, words.Length, "No Duplicate Index Created");
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
            Assert.AreEqual(IndexBuildResults.Successful, indexBuilder.CreateIndex(new FileInfo(oldFileName)));
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
            Assert.AreEqual(IndexBuildResults.Successful, indexBuilder.CreateIndex(new FileInfo(oldFileName)));
            Assert.AreEqual(oldFileName, indexBuilder.CodeIndexPool.SearchCode(new MatchAllDocsQuery()).First().FilePath);

            Directory.Move(oldFileName, newFileName);
            Assert.AreEqual(IndexBuildResults.Successful, indexBuilder.RenameFileIndex(oldFileName, newFileName));
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
            File.AppendAllText(fileName, "ABCD Eeee EEEE");
            Assert.AreEqual(IndexBuildResults.Successful, indexBuilder.CreateIndex(new FileInfo(fileName)));
            Assert.AreEqual("ABCD Eeee EEEE", indexBuilder.CodeIndexPool.SearchCode(new MatchAllDocsQuery()).First().Content);
            CollectionAssert.AreEquivalent(new[] { "ABCD", "Eeee", "EEEE" }, indexBuilder.HintIndexPool.SearchWord(new MatchAllDocsQuery()).Select(u => u.Word));

            File.Delete(fileName);
            File.AppendAllText(fileName, "WOWO IT IS NEW CONTENT APPLY ABCD EEEE");
            Assert.AreEqual(IndexBuildResults.Successful, indexBuilder.UpdateIndex(new FileInfo(fileName), CancellationToken.None));
            Assert.AreEqual("WOWO IT IS NEW CONTENT APPLY ABCD EEEE", indexBuilder.CodeIndexPool.SearchCode(new MatchAllDocsQuery()).First().Content);
            CollectionAssert.AreEquivalent(new[] { "WOWO", "ABCD", "CONTENT", "APPLY", "EEEE" }, indexBuilder.HintIndexPool.SearchWord(new MatchAllDocsQuery()).Select(u => u.Word), "Words Been Added And Removed If Needed");

            var fileName2 = Path.Combine(MonitorFolder, "B.txt");
            File.AppendAllText(fileName2, "ABCD Eeee");
            Assert.AreEqual(IndexBuildResults.Successful, indexBuilder.CreateIndex(new FileInfo(fileName2)));
            CollectionAssert.AreEquivalent(new[] { "WOWO", "ABCD", "CONTENT", "APPLY", "Eeee", "EEEE" }, indexBuilder.HintIndexPool.SearchWord(new MatchAllDocsQuery()).Select(u => u.Word), "Words Been Added And Removed If Needed");

            File.Delete(fileName2);
            File.AppendAllText(fileName2, "Eeee");
            CollectionAssert.AreEquivalent(new[] { "WOWO", "ABCD", "CONTENT", "APPLY", "Eeee", "EEEE" }, indexBuilder.HintIndexPool.SearchWord(new MatchAllDocsQuery()).Select(u => u.Word), "Words Been Added And Removed If Needed");
        }

        [Test]
        public void TestDeleteIndex()
        {
            using var indexBuilder = new CodeIndexBuilder("ABC", new LucenePoolLight(TempCodeIndexDir), new LucenePoolLight(TempHintIndexDir), Log);

            File.AppendAllText(Path.Combine(MonitorFolder, "A.txt"), "ABCD EEEE dddd");
            File.AppendAllText(Path.Combine(MonitorFolder, "B.txt"), "ABCD DDDD eeEE");
            Assert.AreEqual(IndexBuildResults.Successful, indexBuilder.CreateIndex(new FileInfo(Path.Combine(MonitorFolder, "A.txt"))));
            Assert.AreEqual(IndexBuildResults.Successful, indexBuilder.CreateIndex(new FileInfo(Path.Combine(MonitorFolder, "B.txt"))));
            Assert.AreEqual(2, indexBuilder.CodeIndexPool.SearchCode(new MatchAllDocsQuery()).Length);
            CollectionAssert.AreEquivalent(new[] { "ABCD", "EEEE", "DDDD", "dddd", "eeEE" }, indexBuilder.HintIndexPool.SearchWord(new MatchAllDocsQuery()).Select(u => u.Word));

            Assert.IsTrue(indexBuilder.DeleteIndex(Path.Combine(MonitorFolder, "A.txt")));
            Assert.AreEqual(1, indexBuilder.CodeIndexPool.SearchCode(new MatchAllDocsQuery()).Length);
            CollectionAssert.AreEquivalent(new[] { "ABCD", "DDDD", "eeEE" }, indexBuilder.HintIndexPool.SearchWord(new MatchAllDocsQuery()).Select(u => u.Word), "Words Been Removed If Needed");
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
            Assert.AreEqual(IndexBuildResults.Successful, indexBuilder.CreateIndex(new FileInfo(Path.Combine(MonitorFolder, "A.txt"))));
            Assert.IsFalse(IndexBuilderHelper.IndexExists(TempCodeIndexDir));

            indexBuilder.Commit();
            Assert.IsTrue(IndexBuilderHelper.IndexExists(TempCodeIndexDir));

            using var dir = Lucene.Net.Store.FSDirectory.Open(TempCodeIndexDir);
            using var reader = DirectoryReader.Open(dir);
            var searcher = new IndexSearcher(reader);
            Assert.AreEqual(1, searcher.Search(new MatchAllDocsQuery(), 1).TotalHits);
        }

        [Test]
        public void TestGetAllHintWords()
        {
            using var indexBuilder = new CodeIndexBuilder("ABC", new LucenePoolLight(TempCodeIndexDir), new LucenePoolLight(TempHintIndexDir), Log);
            File.AppendAllText(Path.Combine(MonitorFolder, "A.txt"), "ABCD EEEE WoWo Eeee Abcd");
            File.AppendAllText(Path.Combine(MonitorFolder, "B.txt"), "Neww Content WoWo skrskrskr");
            Assert.AreEqual(IndexBuildResults.Successful, indexBuilder.CreateIndex(new FileInfo(Path.Combine(MonitorFolder, "A.txt"))));
            Assert.AreEqual(IndexBuildResults.Successful, indexBuilder.CreateIndex(new FileInfo(Path.Combine(MonitorFolder, "B.txt"))));
            CollectionAssert.AreEquivalent(new[] { "ABCD", "EEEE", "WoWo", "Eeee", "Abcd", "Neww", "Content", "skrskrskr" }, indexBuilder.GetAllHintWords());
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

            var failedFiles = indexBuilder.BuildIndexByBatch(new[] { new FileInfo(fileName1), new FileInfo(fileName2) }, true, true, true, CancellationToken.None, true);
            CollectionAssert.IsEmpty(failedFiles);
            Assert.AreEqual(2, indexBuilder.CodeIndexPool.SearchCode(new MatchAllDocsQuery()).Length);
            Assert.AreEqual(2, indexBuilder.HintIndexPool.SearchWord(new MatchAllDocsQuery()).Length);

            indexBuilder.DeleteAllIndex();
            Assert.AreEqual(0, indexBuilder.CodeIndexPool.SearchCode(new MatchAllDocsQuery()).Length);
            Assert.AreEqual(0, indexBuilder.HintIndexPool.SearchWord(new MatchAllDocsQuery()).Length);
        }

        [Test]
        public void TestNotBrandNewBuild()
        {
            using var indexBuilder = new CodeIndexBuilder("ABC", new LucenePoolLight(TempCodeIndexDir), new LucenePoolLight(TempHintIndexDir), Log);
            Assert.IsFalse(Directory.Exists(TempCodeIndexDir));
            Assert.IsFalse(Directory.Exists(TempHintIndexDir));

            var fileName1 = Path.Combine(MonitorFolder, "A.txt");
            var fileName2 = Path.Combine(MonitorFolder, "B.txt");
            File.AppendAllText(fileName1, "ABCD ABCD");
            File.AppendAllText(fileName2, "ABCD EFGH");

            var failedFiles = indexBuilder.BuildIndexByBatch(new[] { new FileInfo(fileName1) }, true, true, true, CancellationToken.None, true);
            CollectionAssert.IsEmpty(failedFiles);
            Assert.AreEqual(1, indexBuilder.CodeIndexPool.SearchCode(new MatchAllDocsQuery()).Length);
            Assert.AreEqual(1, indexBuilder.HintIndexPool.SearchWord(new MatchAllDocsQuery()).Length);

            indexBuilder.BuildIndexByBatch(new[] { new FileInfo(fileName2) }, true, true, true, CancellationToken.None, false);
            CollectionAssert.IsEmpty(failedFiles);
            Assert.AreEqual(2, indexBuilder.CodeIndexPool.SearchCode(new MatchAllDocsQuery()).Length);
            Assert.AreEqual(2, indexBuilder.HintIndexPool.SearchWord(new MatchAllDocsQuery()).Length, "When not brand new build, will do update hint index rather than add without check");
        }

        [Test]
        public void TestGetCodeSourceFromDocument()
        {
            using var indexBuilder = new CodeIndexBuilder("ABC", new LucenePoolLight(TempCodeIndexDir), new LucenePoolLight(TempHintIndexDir), Log);
            var fileName = Path.Combine(MonitorFolder, "A.txt");
            File.AppendAllText(fileName, "ABCD ABCD");
            var fileInfo = new FileInfo(fileName);
            Thread.Sleep(1);

            Assert.AreEqual(IndexBuildResults.Successful, indexBuilder.CreateIndex(new FileInfo(fileName)));
            var codeSources = indexBuilder.CodeIndexPool.Search(new MatchAllDocsQuery(), 1).Select(CodeIndexBuilder.GetCodeSourceFromDocument).ToArray();
            Assert.AreEqual(1, codeSources.Length);
            Assert.AreEqual(fileName, codeSources[0].FilePath);
            Assert.AreEqual("A.txt", codeSources[0].FileName);
            Assert.AreEqual("txt", codeSources[0].FileExtension);
            Assert.AreEqual("ABCD ABCD", codeSources[0].Content);
            Assert.AreNotEqual(Guid.Empty, new Guid(codeSources[0].CodePK));
            Assert.AreEqual(fileInfo.LastWriteTimeUtc, codeSources[0].LastWriteTimeUtc);
            Assert.LessOrEqual(codeSources[0].LastWriteTimeUtc, codeSources[0].IndexDate);
        }

        [Test]
        public void TestGetAllIndexedCodeSource()
        {
            using var indexBuilder = new CodeIndexBuilder("ABC", new LucenePoolLight(TempCodeIndexDir), new LucenePoolLight(TempHintIndexDir), Log);
            var fileName1 = Path.Combine(MonitorFolder, "A.txt");
            var fileName2 = Path.Combine(MonitorFolder, "B.txt");
            File.AppendAllText(fileName1, "ABCD ABCD");
            File.AppendAllText(fileName2, "EFGH");

            var file1Info = new FileInfo(fileName1);
            var file2Info = new FileInfo(fileName2);

            var failedFiles = indexBuilder.BuildIndexByBatch(new[] { new FileInfo(fileName1), new FileInfo(fileName2) }, true, true, true, CancellationToken.None, true);
            CollectionAssert.IsEmpty(failedFiles);

            CollectionAssert.AreEquivalent(new[] { (file1Info.FullName, file1Info.LastWriteTimeUtc), (file2Info.FullName, file2Info.LastWriteTimeUtc) }, indexBuilder.GetAllIndexedCodeSource());
        }

        QueryGenerator generator;
        QueryGenerator Generator => generator ??= new QueryGenerator(
            new QueryParser(Constants.AppLuceneVersion, nameof(CodeSource.Content), new CodeAnalyzer(Constants.AppLuceneVersion, true)),
            new QueryParser(Constants.AppLuceneVersion, nameof(CodeSource.Content), new CodeAnalyzer(Constants.AppLuceneVersion, true))
            {
                LowercaseExpandedTerms = false
            });
    }
}
