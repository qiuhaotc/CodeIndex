using System;
using System.IO;
using System.Linq;
using System.Threading;
using CodeIndex.Common;
using CodeIndex.MaintainIndex;
using CodeIndex.Search;
using NUnit.Framework;

namespace CodeIndex.Test
{
    public class CodeIndexBuilderTest : BaseTest
    {
        [Test]
        public void TestBuildIndex()
        {
            var logger = new DummyLog();
            using var indexManagement = new IndexManagement(Config, new DummyLog());
            var indexSearcher = new CodeIndexSearcherLight(indexManagement, logger);

            var config = BuildIndex(indexManagement);

            var result1 = indexSearcher.SearchCode($"{nameof(CodeSource.FileName)}:\"Dummy File\"", out var query, 10, config.Pk);
            Assert.That(result1.Length, Is.EqualTo(1));
            Assert.AreEqual(@"Dummy File.txt", result1[0].FileName);
            Assert.AreEqual(@"txt", result1[0].FileExtension);
            Assert.AreEqual(Path.Combine(MonitorFolder, "Dummy File.txt"), result1[0].FilePath);
            Assert.AreEqual("Test Content" + Environment.NewLine + "A New Line For Test", result1[0].Content);
            Assert.GreaterOrEqual(DateTime.UtcNow, result1[0].IndexDate);

            var result2 = indexSearcher.SearchCode("BBBB test", out query, 10, config.Pk);
            Assert.That(result2.Length, Is.EqualTo(2));
            Assert.IsTrue(result2.Any(u => u.FileName == "Dummy File.txt"));
            Assert.IsTrue(result2.Any(u => u.FileName == "A new File.xml"));

            var result3 = indexSearcher.SearchCode("BBBB", out query, 10, config.Pk);
            Assert.That(result3.Length, Is.EqualTo(1));
            Assert.IsTrue(result3.Any(u => u.FileName == "A new File.xml"));
        }

        //[Test]
        //public void TestBuildIndex_DeleteOldIndexWithSamePath()
        //{
        //    BuildIndex();
        //    LucenePool.SaveResultsAndClearLucenePool(Config.LuceneIndexForCode);

        //    var result = CodeIndexSearcher.Search(Config.LuceneIndexForCode, Generator.GetQueryFromStr($"{nameof(CodeSource.FileName)}:\"Dummy File\""), 10);
        //    Assert.AreEqual("Test Content" + Environment.NewLine + "A New Line For Test", result.Single().Get(nameof(CodeSource.Content)));

        //    IndexBuilderHelper.BuildIndex(Config, true, true, true, new[] { new CodeSource
        //    {
        //        FileName = "Dummy File New",
        //        FileExtension = "cs",
        //        FilePath = @"C:\Dummy File.cs",
        //        Content = "ABC",
        //        IndexDate = new DateTime(2020, 1, 1)
        //    }});
        //    LucenePool.SaveResultsAndClearLucenePool(Config.LuceneIndexForCode);

        //    result = CodeIndexSearcher.Search(Config.LuceneIndexForCode, Generator.GetQueryFromStr($"{nameof(CodeSource.FileName)}:\"Dummy File New\""), 10);
        //    Assert.AreEqual("ABC", result.Single().Get(nameof(CodeSource.Content)));
        //}

        //[Test]
        //public void TestDeleteIndex()
        //{
        //    BuildIndex();
        //    LucenePool.SaveResultsAndClearLucenePool(Config.LuceneIndexForCode);

        //    var generator = Generator;
        //    var result = CodeIndexSearcher.Search(Config.LuceneIndexForCode, generator.GetQueryFromStr("FFFF test"), 10);
        //    Assert.That(result.Length, Is.EqualTo(2));

        //    IndexBuilderHelper.DeleteIndex(Config.LuceneIndexForCode, new Term(nameof(CodeSource.FileExtension), "xml"));
        //    LucenePool.SaveResultsAndClearLucenePool(Config.LuceneIndexForCode);
        //    result = CodeIndexSearcher.Search(Config.LuceneIndexForCode, generator.GetQueryFromStr("FFFF test"), 10);
        //    Assert.That(result.Length, Is.EqualTo(1));

        //    IndexBuilderHelper.DeleteIndex(Config.LuceneIndexForCode, generator.GetQueryFromStr("Test"));
        //    LucenePool.SaveResultsAndClearLucenePool(Config.LuceneIndexForCode);
        //    result = CodeIndexSearcher.Search(Config.LuceneIndexForCode, generator.GetQueryFromStr("FFFF test"), 10);
        //    Assert.That(result.Length, Is.EqualTo(0));
        //}

        //[Test]
        //public void TestUpdateIndex()
        //{
        //    BuildIndex();
        //    LucenePool.SaveResultsAndClearLucenePool(Config.LuceneIndexForCode);

        //    var result = CodeIndexSearcher.Search(Config.LuceneIndexForCode, new TermQuery(new Term(nameof(CodeSource.FilePath) + Constants.NoneTokenizeFieldSuffix, @"D:\DDDD\A new Name.cs")), 10);
        //    Assert.That(result.Length, Is.EqualTo(1));

        //    IndexBuilderHelper.UpdateIndex(Config.LuceneIndexForCode, new Term(nameof(CodeSource.FilePath) + Constants.NoneTokenizeFieldSuffix, @"d:\dddd\a new name.cs"), IndexBuilderHelper.GetDocumentFromSource(new CodeSource()
        //    {
        //        Content = "AAA",
        //        FileExtension = "CCC",
        //        FilePath = "BBB",
        //        FileName = "DDD",
        //        IndexDate = new DateTime(1999, 12, 31),
        //        LastWriteTimeUtc = new DateTime(2000, 1, 1)
        //    }));
        //    LucenePool.SaveResultsAndClearLucenePool(Config.LuceneIndexForCode);

        //    result = CodeIndexSearcher.Search(Config.LuceneIndexForCode, new TermQuery(new Term(nameof(CodeSource.FilePath) + Constants.NoneTokenizeFieldSuffix, @"d:\dddd\a new name.cs")), 10);
        //    Assert.That(result.Length, Is.EqualTo(0));

        //    result = CodeIndexSearcher.Search(Config.LuceneIndexForCode, new TermQuery(new Term(nameof(CodeSource.FilePath) + Constants.NoneTokenizeFieldSuffix, "BBB")), 10);
        //    Assert.That(result.Length, Is.EqualTo(1));
        //    Assert.AreEqual(@"DDD", result[0].Get(nameof(CodeSource.FileName)));
        //    Assert.AreEqual(@"CCC", result[0].Get(nameof(CodeSource.FileExtension)));
        //    Assert.AreEqual(@"BBB", result[0].Get(nameof(CodeSource.FilePath)));
        //    Assert.AreEqual("AAA", result[0].Get(nameof(CodeSource.Content)));
        //    Assert.AreEqual(new DateTime(1999, 12, 31).Ticks, result[0].GetField(nameof(CodeSource.IndexDate)).GetInt64Value());
        //    Assert.AreEqual(new DateTime(2000, 1, 1).Ticks, result[0].GetField(nameof(CodeSource.LastWriteTimeUtc)).GetInt64Value());
        //}

        //[Test]
        //public void TestIndexExists()
        //{
        //    Assert.IsFalse(IndexBuilderHelper.IndexExists(Config.LuceneIndexForCode));

        //    BuildIndex();

        //    Assert.IsFalse(IndexBuilderHelper.IndexExists(Config.LuceneIndexForCode));

        //    LucenePool.SaveResultsAndClearLucenePool(Config.LuceneIndexForCode);

        //    Assert.IsTrue(IndexBuilderHelper.IndexExists(Config.LuceneIndexForCode));
        //}

        //[Test]
        //public void TestDeleteAllIndex()
        //{
        //    BuildIndex();
        //    LucenePool.SaveResultsAndClearLucenePool(Config.LuceneIndexForCode);
        //    Assert.AreEqual(1, CodeIndexSearcher.Search(Config.LuceneIndexForCode, Generator.GetQueryFromStr($"{nameof(CodeSource.FileName)}:\"Dummy File\""), 10).Length);
        //    Assert.AreEqual(1, CodeIndexSearcher.Search(Config.LuceneIndexForCode, Generator.GetQueryFromStr($"{nameof(CodeSource.FileName)}:\"A new File\""), 10).Length);

        //    IndexBuilderHelper.DeleteAllIndex(Config);
        //    Assert.AreEqual(0, CodeIndexSearcher.Search(Config.LuceneIndexForCode, Generator.GetQueryFromStr($"{nameof(CodeSource.FileName)}:\"Dummy File\""), 10).Length);
        //    Assert.AreEqual(0, CodeIndexSearcher.Search(Config.LuceneIndexForCode, Generator.GetQueryFromStr($"{nameof(CodeSource.FileName)}:\"A new File\""), 10).Length);
        //}

        //[Test]
        //public void TestUpdateCodeFilePath()
        //{
        //    var document = IndexBuilderHelper.GetDocumentFromSource(new CodeSource
        //    {
        //        Content = "AAA",
        //        FileExtension = "CCC",
        //        FilePath = "BBB/DDD/1.txt",
        //        FileName = "1.txt",
        //        IndexDate = new DateTime(1999, 12, 31),
        //        LastWriteTimeUtc = new DateTime(2000, 1, 1)
        //    });

        //    IndexBuilderHelper.UpdateCodeFilePath(document, "BBB/DDD/", "AAA/EEE/");
        //    Assert.AreEqual(@"AAA/EEE/1.txt", document.Get(nameof(CodeSource.FilePath)));
        //    Assert.AreEqual(@"AAA/EEE/1.txt", document.Get(nameof(CodeSource.FilePath) + Constants.NoneTokenizeFieldSuffix));
        //}

        //[Test]
        //public void TestBuildIndexByBatch_ReturnFailedFiles()
        //{
        //    File.WriteAllText(Path.Combine(TempDir, "A.txt"), "ABCD");
        //    IndexBuilderHelper.BuildIndexByBatch(Config, true, true, true, new[] { new FileInfo(Path.Combine(TempDir, "A.txt")) }, false, new DummyLog { ThrowExceptionWhenLogContains = "Add index For " + Path.Combine(TempDir, "A.txt") }, out var failedIndexFiles);
        //    Assert.AreEqual(1, failedIndexFiles.Count);

        //    IndexBuilderHelper.BuildIndexByBatch(Config, true, true, true, new[] { new FileInfo(Path.Combine(TempDir, "A.txt")), new FileInfo("BlaBla\\a.txt") }, false, null, out failedIndexFiles);
        //    Assert.AreEqual(0, failedIndexFiles.Count);
        //}

        //[Test]
        //public void TestInitIndexFolderIfNeeded()
        //{
        //    Assert.IsFalse(Directory.Exists(Config.LuceneIndexForCode));
        //    Assert.IsFalse(Directory.Exists(Config.LuceneIndexForHint));

        //    IndexBuilderHelper.InitIndexFolderIfNeeded(Config, null);

        //    Assert.IsTrue(Directory.Exists(Config.LuceneIndexForCode));
        //    Assert.IsTrue(Directory.Exists(Config.LuceneIndexForHint));
        //}

        IndexConfig BuildIndex(IndexManagement indexManagement)
        {
            var indexConfig = new IndexConfig
            {
                IndexName = "dummy",
                MonitorFolder = MonitorFolder
            };

            indexManagement.AddIndex(indexConfig);

            File.AppendAllText(Path.Combine(MonitorFolder, "Dummy File.txt"), "Test Content" + Environment.NewLine + "A New Line For Test");
            File.AppendAllText(Path.Combine(MonitorFolder, "A new File.xml"), "BBBB Content A new Line");

            var result = indexManagement.StartIndex(indexConfig.Pk);

            Assert.IsTrue(result.Status.Success);

            while (indexManagement.GetIndexList().Result.First(u => u.IndexConfig == indexConfig).IndexStatus != IndexStatus.Monitoring)
            {
                Thread.Sleep(100);
            }

            return indexConfig;
        }
    }
}
