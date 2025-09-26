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
            Assert.That(Directory.Exists(TempCodeIndexDir), Is.False);
            Assert.That(Directory.Exists(TempHintIndexDir), Is.False);

            indexBuilder.InitIndexFolderIfNeeded();
            Assert.That(Directory.Exists(TempCodeIndexDir), Is.True);
            Assert.That(Directory.Exists(TempHintIndexDir), Is.True);
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
            Assert.That(failedFiles, Is.Empty);

            var results = indexBuilder.CodeIndexPool.SearchCode(Generator.GetQueryFromStr("ABCD", false));

            Assert.That(results.Length, Is.EqualTo(2));
            Assert.That(results[0].FileName, Is.EqualTo("A.txt"));
            Assert.That(results[0].FileExtension, Is.EqualTo("txt"));
            Assert.That(results[0].FilePath, Is.EqualTo(fileName1));
            Assert.That(results[0].Content, Is.EqualTo("ABCD ABCD" + Environment.NewLine + "ABCD"));
            Assert.That(DateTime.UtcNow, Is.GreaterThanOrEqualTo(results[0].IndexDate));
            Assert.That(results[1].FileName, Is.EqualTo("B.txt"));
            Assert.That(results[1].FileExtension, Is.EqualTo("txt"));
            Assert.That(results[1].FilePath, Is.EqualTo(fileName2));
            Assert.That(results[1].Content, Is.EqualTo("ABCD EFGH"));
            Assert.That(DateTime.UtcNow, Is.GreaterThanOrEqualTo(results[1].IndexDate));
            Assert.That(indexBuilder.CodeIndexPool.SearchCode(Generator.GetQueryFromStr("EFGH", false)).Length, Is.EqualTo(1));
            Assert.That(indexBuilder.HintIndexPool.SearchWord(new TermQuery(new Term(nameof(CodeWord.Word), "ABCD"))).Length, Is.EqualTo(1));
            Assert.That(indexBuilder.HintIndexPool.SearchWord(new TermQuery(new Term(nameof(CodeWord.Word), "EFGH"))).Length, Is.EqualTo(1));
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
                Assert.That(failedFiles.Select(u => u.FullName), Is.EquivalentTo(new[] { fileName1 }));
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
            Assert.That(indexBuilder.CreateIndex(new FileInfo(fileName)), Is.EqualTo(IndexBuildResults.Successful));

            var results = indexBuilder.CodeIndexPool.SearchCode(new MatchAllDocsQuery());
            var words = indexBuilder.HintIndexPool.SearchWord(new MatchAllDocsQuery());
            Assert.That(results.Length, Is.EqualTo(1));
            Assert.That(words.Length, Is.EqualTo(2));
            Assert.That(results[0].FileName, Is.EqualTo("A.txt"));
            Assert.That(results[0].FileExtension, Is.EqualTo("txt"));
            Assert.That(results[0].FilePath, Is.EqualTo(fileName));
            Assert.That(results[0].Content, Is.EqualTo("ABCD EEEE"));
            Assert.That(DateTime.UtcNow, Is.GreaterThanOrEqualTo(results[0].IndexDate));
            Assert.That(words.Select(u => u.Word), Is.EquivalentTo(new[] { "ABCD", "EEEE" }));

            Assert.That(indexBuilder.CreateIndex(new FileInfo(fileName)), Is.EqualTo(IndexBuildResults.Successful));
            results = indexBuilder.CodeIndexPool.SearchCode(new MatchAllDocsQuery());
            words = indexBuilder.HintIndexPool.SearchWord(new MatchAllDocsQuery());
            Assert.That(results.Length, Is.EqualTo(1), "No Duplicate Index Created");
            Assert.That(words.Length, Is.EqualTo(2), "No Duplicate Index Created");
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
            Assert.That(indexBuilder.CreateIndex(new FileInfo(oldFileName)), Is.EqualTo(IndexBuildResults.Successful));
            Assert.That(indexBuilder.CodeIndexPool.SearchCode(new MatchAllDocsQuery()).First().FilePath, Is.EqualTo(oldFileName));

            Directory.Move(oldPath, newPath);
            Assert.That(indexBuilder.RenameFolderIndexes(oldPath, newPath, CancellationToken.None), Is.True);
            Assert.That(indexBuilder.CodeIndexPool.SearchCode(new MatchAllDocsQuery()).First().FilePath, Is.EqualTo(newFileName));

            var results = indexBuilder.CodeIndexPool.SearchCode(new PrefixQuery(indexBuilder.GetNoneTokenizeFieldTerm(nameof(CodeSource.FilePath), newPath)));
            Assert.That(results.Length, Is.EqualTo(1));
            Assert.That(results[0].FilePath, Is.EqualTo(newFileName));
            Assert.That(results[0].Content, Is.EqualTo("ABCD EEEE"));
        }

        [Test]
        public void TestRenameFileIndex()
        {
            using var indexBuilder = new CodeIndexBuilder("ABC", new LucenePoolLight(TempCodeIndexDir), new LucenePoolLight(TempHintIndexDir), Log);

            var oldFileName = Path.Combine(MonitorFolder, "A.TXT");
            var newFileName = Path.Combine(MonitorFolder, "B.SQL");
            File.AppendAllText(oldFileName, "ABCD EEEE");
            Assert.That(indexBuilder.CreateIndex(new FileInfo(oldFileName)), Is.EqualTo(IndexBuildResults.Successful));

            var results = indexBuilder.CodeIndexPool.SearchCode(new TermQuery(indexBuilder.GetNoneTokenizeFieldTerm(nameof(CodeSource.FilePath), oldFileName)));
            Assert.That(results[0].FileExtension, Is.EqualTo("txt"));
            Assert.That(results[0].FileName, Is.EqualTo("A.TXT"));
            Assert.That(results[0].Content, Is.EqualTo("ABCD EEEE"));
            Assert.That(results[0].FilePath, Is.EqualTo(oldFileName));

            Directory.Move(oldFileName, newFileName);
            Assert.That(indexBuilder.RenameFileIndex(oldFileName, newFileName), Is.EqualTo(IndexBuildResults.Successful));

            results = indexBuilder.CodeIndexPool.SearchCode(new TermQuery(indexBuilder.GetNoneTokenizeFieldTerm(nameof(CodeSource.FilePath), newFileName)));
            Assert.That(results.Length, Is.EqualTo(1));
            Assert.That(results[0].FileExtension, Is.EqualTo("sql"));
            Assert.That(results[0].FileName, Is.EqualTo("B.SQL"));
            Assert.That(results[0].Content, Is.EqualTo("ABCD EEEE"));
            Assert.That(results[0].FilePath, Is.EqualTo(newFileName));
        }

        [Test]
        public void TestUpdateIndex()
        {
            using var indexBuilder = new CodeIndexBuilder("ABC", new LucenePoolLight(TempCodeIndexDir), new LucenePoolLight(TempHintIndexDir), Log);

            var fileName = Path.Combine(MonitorFolder, "A.txt");
            File.AppendAllText(fileName, "ABCD Eeee EEEE");
            Assert.That(indexBuilder.CreateIndex(new FileInfo(fileName)), Is.EqualTo(IndexBuildResults.Successful));
            Assert.That(indexBuilder.CodeIndexPool.SearchCode(new MatchAllDocsQuery()).First().Content, Is.EqualTo("ABCD Eeee EEEE"));
            Assert.That(indexBuilder.HintIndexPool.SearchWord(new MatchAllDocsQuery()).Select(u => u.Word), Is.EquivalentTo(new[] { "ABCD", "Eeee", "EEEE" }));

            File.Delete(fileName);
            File.AppendAllText(fileName, "WOWO IT IS NEW CONTENT APPLY ABCD EEEE");
            Assert.That(indexBuilder.UpdateIndex(new FileInfo(fileName), CancellationToken.None), Is.EqualTo(IndexBuildResults.Successful));
            Assert.That(indexBuilder.CodeIndexPool.SearchCode(new MatchAllDocsQuery()).First().Content, Is.EqualTo("WOWO IT IS NEW CONTENT APPLY ABCD EEEE"));
            Assert.That(indexBuilder.HintIndexPool.SearchWord(new MatchAllDocsQuery()).Select(u => u.Word), Is.EquivalentTo(new[] { "WOWO", "ABCD", "CONTENT", "APPLY", "EEEE" }), "Words Been Added And Removed If Needed");

            var fileName2 = Path.Combine(MonitorFolder, "B.txt");
            File.AppendAllText(fileName2, "ABCD Eeee");
            Assert.That(indexBuilder.CreateIndex(new FileInfo(fileName2)), Is.EqualTo(IndexBuildResults.Successful));
            Assert.That(indexBuilder.HintIndexPool.SearchWord(new MatchAllDocsQuery()).Select(u => u.Word), Is.EquivalentTo(new[] { "WOWO", "ABCD", "CONTENT", "APPLY", "Eeee", "EEEE" }), "Words Been Added And Removed If Needed");

            File.Delete(fileName2);
            File.AppendAllText(fileName2, "Eeee");
            Assert.That(indexBuilder.HintIndexPool.SearchWord(new MatchAllDocsQuery()).Select(u => u.Word), Is.EquivalentTo(new[] { "WOWO", "ABCD", "CONTENT", "APPLY", "Eeee", "EEEE" }), "Words Been Added And Removed If Needed");
        }

        [Test]
        public void TestDeleteIndex()
        {
            using var indexBuilder = new CodeIndexBuilder("ABC", new LucenePoolLight(TempCodeIndexDir), new LucenePoolLight(TempHintIndexDir), Log);

            File.AppendAllText(Path.Combine(MonitorFolder, "A.txt"), "ABCD EEEE dddd");
            File.AppendAllText(Path.Combine(MonitorFolder, "B.txt"), "ABCD DDDD eeEE");
            Assert.That(indexBuilder.CreateIndex(new FileInfo(Path.Combine(MonitorFolder, "A.txt"))), Is.EqualTo(IndexBuildResults.Successful));
            Assert.That(indexBuilder.CreateIndex(new FileInfo(Path.Combine(MonitorFolder, "B.txt"))), Is.EqualTo(IndexBuildResults.Successful));
            Assert.That(indexBuilder.CodeIndexPool.SearchCode(new MatchAllDocsQuery()).Length, Is.EqualTo(2));
            Assert.That(indexBuilder.HintIndexPool.SearchWord(new MatchAllDocsQuery()).Select(u => u.Word), Is.EquivalentTo(new[] { "ABCD", "EEEE", "DDDD", "dddd", "eeEE" }));

            Assert.That(indexBuilder.DeleteIndex(Path.Combine(MonitorFolder, "A.txt")), Is.True);
            Assert.That(indexBuilder.CodeIndexPool.SearchCode(new MatchAllDocsQuery()).Length, Is.EqualTo(1));
            Assert.That(indexBuilder.HintIndexPool.SearchWord(new MatchAllDocsQuery()).Select(u => u.Word), Is.EquivalentTo(new[] { "ABCD", "DDDD", "eeEE" }), "Words Been Removed If Needed");
        }

        [Test]
        public void TestDeleteIndex_FolderDelete()
        {
            using var indexBuilder = new CodeIndexBuilder("ABC", new LucenePoolLight(TempCodeIndexDir), new LucenePoolLight(TempHintIndexDir), Log);

            Directory.CreateDirectory(Path.Combine(MonitorFolder, "SubDir"));
            File.AppendAllText(Path.Combine(MonitorFolder, "SubDir.txt"), "1234");
            File.AppendAllText(Path.Combine(MonitorFolder, "SubDir", "A.txt"), "5678");
            File.AppendAllText(Path.Combine(MonitorFolder, "SubDir", "B.txt"), "5678");

            Assert.That(indexBuilder.CreateIndex(new FileInfo(Path.Combine(MonitorFolder, "SubDir.txt"))), Is.EqualTo(IndexBuildResults.Successful));
            Assert.That(indexBuilder.CreateIndex(new FileInfo(Path.Combine(MonitorFolder, "SubDir", "A.txt"))), Is.EqualTo(IndexBuildResults.Successful));
            Assert.That(indexBuilder.CreateIndex(new FileInfo(Path.Combine(MonitorFolder, "SubDir", "B.txt"))), Is.EqualTo(IndexBuildResults.Successful));
            Assert.That(indexBuilder.CodeIndexPool.SearchCode(new MatchAllDocsQuery()).Length, Is.EqualTo(3));

            Assert.That(indexBuilder.DeleteIndex(Path.Combine(MonitorFolder, "SubDir")), Is.True);

            var results = indexBuilder.CodeIndexPool.SearchCode(new MatchAllDocsQuery());
            Assert.That(results.Length, Is.EqualTo(1), "Should not delete the index of SubDir.txt");
            Assert.That(results[0].FileName, Is.EqualTo("SubDir.txt"));
        }

        [Test]
        public void TestGetNoneTokenizeFieldTerm()
        {
            using var indexBuilder = new CodeIndexBuilder("ABC", new LucenePoolLight(TempCodeIndexDir), new LucenePoolLight(TempHintIndexDir), Log);

            var term = indexBuilder.GetNoneTokenizeFieldTerm("ABC", "Value");
            Assert.That(term.Field, Is.EqualTo("ABC" + Constants.NoneTokenizeFieldSuffix));
            Assert.That(term.Text, Is.EqualTo("Value"));
        }

        [Test]
        public void TestCommit()
        {
            using var indexBuilder = new CodeIndexBuilder("ABC", new LucenePoolLight(TempCodeIndexDir), new LucenePoolLight(TempHintIndexDir), Log);
            File.AppendAllText(Path.Combine(MonitorFolder, "A.txt"), "ABCD EEEE");
            Assert.That(indexBuilder.CreateIndex(new FileInfo(Path.Combine(MonitorFolder, "A.txt"))), Is.EqualTo(IndexBuildResults.Successful));
            Assert.That(IndexBuilderHelper.IndexExists(TempCodeIndexDir), Is.False);

            indexBuilder.Commit();
            Assert.That(IndexBuilderHelper.IndexExists(TempCodeIndexDir), Is.True);

            using var dir = Lucene.Net.Store.FSDirectory.Open(TempCodeIndexDir);
            using var reader = DirectoryReader.Open(dir);
            var searcher = new IndexSearcher(reader);
            Assert.That(searcher.Search(new MatchAllDocsQuery(), 1).TotalHits, Is.EqualTo(1));
        }

        [Test]
        public void TestGetAllHintWords()
        {
            using var indexBuilder = new CodeIndexBuilder("ABC", new LucenePoolLight(TempCodeIndexDir), new LucenePoolLight(TempHintIndexDir), Log);
            File.AppendAllText(Path.Combine(MonitorFolder, "A.txt"), "ABCD EEEE WoWo Eeee Abcd");
            File.AppendAllText(Path.Combine(MonitorFolder, "B.txt"), "Neww Content WoWo skrskrskr");
            Assert.That(indexBuilder.CreateIndex(new FileInfo(Path.Combine(MonitorFolder, "A.txt"))), Is.EqualTo(IndexBuildResults.Successful));
            Assert.That(indexBuilder.CreateIndex(new FileInfo(Path.Combine(MonitorFolder, "B.txt"))), Is.EqualTo(IndexBuildResults.Successful));
            Assert.That(indexBuilder.GetAllHintWords(), Is.EquivalentTo(new[] { "ABCD", "EEEE", "WoWo", "Eeee", "Abcd", "Neww", "Content", "skrskrskr" }));
        }

        [Test]
        public void TestDeleteAllIndex()
        {
            using var indexBuilder = new CodeIndexBuilder("ABC", new LucenePoolLight(TempCodeIndexDir), new LucenePoolLight(TempHintIndexDir), Log);
            Assert.That(Directory.Exists(TempCodeIndexDir), Is.False);
            Assert.That(Directory.Exists(TempHintIndexDir), Is.False);

            var fileName1 = Path.Combine(MonitorFolder, "A.txt");
            var fileName2 = Path.Combine(MonitorFolder, "B.txt");
            File.AppendAllText(fileName1, "ABCD ABCD");
            File.AppendAllText(fileName2, "ABCD EFGH");

            var failedFiles = indexBuilder.BuildIndexByBatch(new[] { new FileInfo(fileName1), new FileInfo(fileName2) }, true, true, true, CancellationToken.None, true);
            Assert.That(failedFiles, Is.Empty);
            Assert.That(indexBuilder.CodeIndexPool.SearchCode(new MatchAllDocsQuery()).Length, Is.EqualTo(2));
            Assert.That(indexBuilder.HintIndexPool.SearchWord(new MatchAllDocsQuery()).Length, Is.EqualTo(2));

            indexBuilder.DeleteAllIndex();
            Assert.That(indexBuilder.CodeIndexPool.SearchCode(new MatchAllDocsQuery()).Length, Is.EqualTo(0));
            Assert.That(indexBuilder.HintIndexPool.SearchWord(new MatchAllDocsQuery()).Length, Is.EqualTo(0));
        }

        [Test]
        public void TestNotBrandNewBuild()
        {
            using var indexBuilder = new CodeIndexBuilder("ABC", new LucenePoolLight(TempCodeIndexDir), new LucenePoolLight(TempHintIndexDir), Log);
            Assert.That(Directory.Exists(TempCodeIndexDir), Is.False);
            Assert.That(Directory.Exists(TempHintIndexDir), Is.False);

            var fileName1 = Path.Combine(MonitorFolder, "A.txt");
            var fileName2 = Path.Combine(MonitorFolder, "B.txt");
            File.AppendAllText(fileName1, "ABCD ABCD");
            File.AppendAllText(fileName2, "ABCD EFGH");

            var failedFiles = indexBuilder.BuildIndexByBatch(new[] { new FileInfo(fileName1) }, true, true, true, CancellationToken.None, true);
            Assert.That(failedFiles, Is.Empty);
            Assert.That(indexBuilder.CodeIndexPool.SearchCode(new MatchAllDocsQuery()).Length, Is.EqualTo(1));
            Assert.That(indexBuilder.HintIndexPool.SearchWord(new MatchAllDocsQuery()).Length, Is.EqualTo(1));

            indexBuilder.BuildIndexByBatch(new[] { new FileInfo(fileName2) }, true, true, true, CancellationToken.None, false);
            Assert.That(failedFiles, Is.Empty);
            Assert.That(indexBuilder.CodeIndexPool.SearchCode(new MatchAllDocsQuery()).Length, Is.EqualTo(2));
            Assert.That(indexBuilder.HintIndexPool.SearchWord(new MatchAllDocsQuery()).Length, Is.EqualTo(2), "When not brand new build, will do update hint index rather than add without check");
        }

        [Test]
        public void TestGetCodeSourceFromDocument()
        {
            using var indexBuilder = new CodeIndexBuilder("ABC", new LucenePoolLight(TempCodeIndexDir), new LucenePoolLight(TempHintIndexDir), Log);
            var fileName = Path.Combine(MonitorFolder, "A.txt");
            File.AppendAllText(fileName, "ABCD ABCD");
            var fileInfo = new FileInfo(fileName);
            Thread.Sleep(1);

            Assert.That(indexBuilder.CreateIndex(new FileInfo(fileName)), Is.EqualTo(IndexBuildResults.Successful));
            var codeSources = indexBuilder.CodeIndexPool.Search(new MatchAllDocsQuery(), 1).Select(CodeIndexBuilder.GetCodeSourceFromDocument).ToArray();
            Assert.That(codeSources.Length, Is.EqualTo(1));
            Assert.That(codeSources[0].FilePath, Is.EqualTo(fileName));
            Assert.That(codeSources[0].FileName, Is.EqualTo("A.txt"));
            Assert.That(codeSources[0].FileExtension, Is.EqualTo("txt"));
            Assert.That(codeSources[0].Content, Is.EqualTo("ABCD ABCD"));
            Assert.That(new Guid(codeSources[0].CodePK), Is.Not.EqualTo(Guid.Empty));
            Assert.That(codeSources[0].LastWriteTimeUtc, Is.EqualTo(fileInfo.LastWriteTimeUtc));
            Assert.That(codeSources[0].LastWriteTimeUtc, Is.LessThanOrEqualTo(codeSources[0].IndexDate));
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
            Assert.That(failedFiles, Is.Empty);

            Assert.That(indexBuilder.GetAllIndexedCodeSource(), Is.EquivalentTo(new[] { (file1Info.FullName, file1Info.LastWriteTimeUtc), (file2Info.FullName, file2Info.LastWriteTimeUtc) }));
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
