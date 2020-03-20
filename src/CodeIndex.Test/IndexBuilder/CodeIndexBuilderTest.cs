using System;
using System.IO;
using System.Linq;
using CodeIndex.Common;
using CodeIndex.IndexBuilder;
using CodeIndex.Search;
using Lucene.Net.Index;
using Lucene.Net.Search;
using NUnit.Framework;

namespace CodeIndex.Test
{
    public class CodeIndexBuilderTest : BaseTest
    {
        [Test]
        public void TestBuildIndex()
        {
            BuildIndex();
            LucenePool.SaveResultsAndClearLucenePool(Config.LuceneIndexForCode);

            var result1 = CodeIndexSearcher.Search(Config.LuceneIndexForCode, Generator.GetQueryFromStr($"{nameof(CodeSource.FileName)}:\"Dummy File\""), 10);
            Assert.That(result1.Length, Is.EqualTo(1));
            Assert.AreEqual(@"Dummy File", result1[0].Get(nameof(CodeSource.FileName)));
            Assert.AreEqual(@"cs", result1[0].Get(nameof(CodeSource.FileExtension)));
            Assert.AreEqual(@"C:\Dummy File.cs", result1[0].Get(nameof(CodeSource.FilePath)));
            Assert.AreEqual(@"C:\Dummy File.cs", result1[0].Get(nameof(CodeSource.FilePath) + Constants.NoneTokenizeFieldSuffix));
            Assert.AreEqual("Test Content" + Environment.NewLine + "A New Line For Test", result1[0].Get(nameof(CodeSource.Content)));
            Assert.AreEqual(new DateTime(2020, 1, 1).Ticks, result1[0].GetField(nameof(CodeSource.IndexDate)).GetInt64Value());

            var generator = new QueryGenerator();
            var result2 = CodeIndexSearcher.Search(Config.LuceneIndexForCode, generator.GetQueryFromStr("FFFF test"), 10);
            Assert.That(result2.Length, Is.EqualTo(2));
            Assert.IsTrue(result2.Any(u => u.Get(nameof(CodeSource.FileName)) == "Dummy File"));
            Assert.IsTrue(result2.Any(u => u.Get(nameof(CodeSource.FileName)) == "A new File"));

            var result3 = CodeIndexSearcher.Search(Config.LuceneIndexForCode, generator.GetQueryFromStr("FFFF"), 10);
            Assert.That(result3.Length, Is.EqualTo(1));
            Assert.IsTrue(result3.Any(u => u.Get(nameof(CodeSource.FileName)) == "A new File"));
        }

        [Test]
        public void TestBuildIndex_DeleteOldIndexWithSamePath()
        {
            BuildIndex();
            LucenePool.SaveResultsAndClearLucenePool(Config.LuceneIndexForCode);

            var result = CodeIndexSearcher.Search(Config.LuceneIndexForCode, Generator.GetQueryFromStr($"{nameof(CodeSource.FileName)}:\"Dummy File\""), 10);
            Assert.AreEqual("Test Content" + Environment.NewLine + "A New Line For Test", result.Single().Get(nameof(CodeSource.Content)));

            CodeIndexBuilder.BuildIndex(Config, true, true, true, new[] { new CodeSource
            {
                FileName = "Dummy File New",
                FileExtension = "cs",
                FilePath = @"C:\Dummy File.cs",
                Content = "ABC",
                IndexDate = new DateTime(2020, 1, 1)
            }});
            LucenePool.SaveResultsAndClearLucenePool(Config.LuceneIndexForCode);

            result = CodeIndexSearcher.Search(Config.LuceneIndexForCode, Generator.GetQueryFromStr($"{nameof(CodeSource.FileName)}:\"Dummy File New\""), 10);
            Assert.AreEqual("ABC", result.Single().Get(nameof(CodeSource.Content)));
        }

        [Test]
        public void TestDeleteIndex()
        {
            BuildIndex();
            LucenePool.SaveResultsAndClearLucenePool(Config.LuceneIndexForCode);

            var generator = new QueryGenerator();
            var result = CodeIndexSearcher.Search(Config.LuceneIndexForCode, generator.GetQueryFromStr("FFFF test"), 10);
            Assert.That(result.Length, Is.EqualTo(2));

            CodeIndexBuilder.DeleteIndex(Config.LuceneIndexForCode, new Term(nameof(CodeSource.FileExtension), "xml"));
            LucenePool.SaveResultsAndClearLucenePool(Config.LuceneIndexForCode);
            result = CodeIndexSearcher.Search(Config.LuceneIndexForCode, generator.GetQueryFromStr("FFFF test"), 10);
            Assert.That(result.Length, Is.EqualTo(1));

            CodeIndexBuilder.DeleteIndex(Config.LuceneIndexForCode, generator.GetQueryFromStr("Test"));
            LucenePool.SaveResultsAndClearLucenePool(Config.LuceneIndexForCode);
            result = CodeIndexSearcher.Search(Config.LuceneIndexForCode, generator.GetQueryFromStr("FFFF test"), 10);
            Assert.That(result.Length, Is.EqualTo(0));
        }

        [Test]
        public void TestUpdateIndex()
        {
            BuildIndex();
            LucenePool.SaveResultsAndClearLucenePool(Config.LuceneIndexForCode);

            var result = CodeIndexSearcher.Search(Config.LuceneIndexForCode, new TermQuery(new Term(nameof(CodeSource.FilePath) + Constants.NoneTokenizeFieldSuffix, @"D:\DDDD\A new Name.cs")), 10);
            Assert.That(result.Length, Is.EqualTo(1));

            CodeIndexBuilder.UpdateIndex(Config.LuceneIndexForCode, new Term(nameof(CodeSource.FilePath) + Constants.NoneTokenizeFieldSuffix, @"d:\dddd\a new name.cs"), CodeIndexBuilder.GetDocumentFromSource(new CodeSource()
            {
                Content = "AAA",
                FileExtension = "CCC",
                FilePath = "BBB",
                FileName = "DDD",
                IndexDate = new DateTime(1999, 12, 31),
                LastWriteTimeUtc = new DateTime(2000, 1, 1)
            }));
            LucenePool.SaveResultsAndClearLucenePool(Config.LuceneIndexForCode);

            result = CodeIndexSearcher.Search(Config.LuceneIndexForCode, new TermQuery(new Term(nameof(CodeSource.FilePath) + Constants.NoneTokenizeFieldSuffix, @"d:\dddd\a new name.cs")), 10);
            Assert.That(result.Length, Is.EqualTo(0));

            result = CodeIndexSearcher.Search(Config.LuceneIndexForCode, new TermQuery(new Term(nameof(CodeSource.FilePath) + Constants.NoneTokenizeFieldSuffix, "BBB")), 10);
            Assert.That(result.Length, Is.EqualTo(1));
            Assert.AreEqual(@"DDD", result[0].Get(nameof(CodeSource.FileName)));
            Assert.AreEqual(@"CCC", result[0].Get(nameof(CodeSource.FileExtension)));
            Assert.AreEqual(@"BBB", result[0].Get(nameof(CodeSource.FilePath)));
            Assert.AreEqual("AAA", result[0].Get(nameof(CodeSource.Content)));
            Assert.AreEqual(new DateTime(1999, 12, 31).Ticks, result[0].GetField(nameof(CodeSource.IndexDate)).GetInt64Value());
            Assert.AreEqual(new DateTime(2000, 1, 1).Ticks, result[0].GetField(nameof(CodeSource.LastWriteTimeUtc)).GetInt64Value());
        }

        [Test]
        public void TestIndexExists()
        {
            Assert.IsFalse(CodeIndexBuilder.IndexExists(Config.LuceneIndexForCode));

            BuildIndex();

            Assert.IsFalse(CodeIndexBuilder.IndexExists(Config.LuceneIndexForCode));

            LucenePool.SaveResultsAndClearLucenePool(Config.LuceneIndexForCode);

            Assert.IsTrue(CodeIndexBuilder.IndexExists(Config.LuceneIndexForCode));
        }

        [Test]
        public void TestDeleteAllIndex()
        {
            BuildIndex();
            LucenePool.SaveResultsAndClearLucenePool(Config.LuceneIndexForCode);
            Assert.AreEqual(1, CodeIndexSearcher.Search(Config.LuceneIndexForCode, Generator.GetQueryFromStr($"{nameof(CodeSource.FileName)}:\"Dummy File\""), 10).Length);
            Assert.AreEqual(1, CodeIndexSearcher.Search(Config.LuceneIndexForCode, Generator.GetQueryFromStr($"{nameof(CodeSource.FileName)}:\"A new File\""), 10).Length);

            CodeIndexBuilder.DeleteAllIndex(Config);
            Assert.AreEqual(0, CodeIndexSearcher.Search(Config.LuceneIndexForCode, Generator.GetQueryFromStr($"{nameof(CodeSource.FileName)}:\"Dummy File\""), 10).Length);
            Assert.AreEqual(0, CodeIndexSearcher.Search(Config.LuceneIndexForCode, Generator.GetQueryFromStr($"{nameof(CodeSource.FileName)}:\"A new File\""), 10).Length);
        }

        [Test]
        public void TestUpdateCodeFilePath()
        {
            var document = CodeIndexBuilder.GetDocumentFromSource(new CodeSource
            {
                Content = "AAA",
                FileExtension = "CCC",
                FilePath = "BBB/DDD/1.txt",
                FileName = "1.txt",
                IndexDate = new DateTime(1999, 12, 31),
                LastWriteTimeUtc = new DateTime(2000, 1, 1)
            });

            CodeIndexBuilder.UpdateCodeFilePath(document, "BBB/DDD/", "AAA/EEE/");
            Assert.AreEqual(@"AAA/EEE/1.txt", document.Get(nameof(CodeSource.FilePath)));
            Assert.AreEqual(@"AAA/EEE/1.txt", document.Get(nameof(CodeSource.FilePath) + Constants.NoneTokenizeFieldSuffix));
        }

        [Test]
        public void TestBuildIndexByBatch_ReturnFailedFiles()
        {
            File.WriteAllText(Path.Combine(TempDir, "A.txt"), "ABCD");
            CodeIndexBuilder.BuildIndexByBatch(Config, true, true, true, new[] { new FileInfo(Path.Combine(TempDir, "A.txt")) }, false, new DummyLog { ThrowExceptionWhenLogContains = "Add index For " + Path.Combine(TempDir, "A.txt") }, out var failedIndexFiles);
            Assert.AreEqual(1, failedIndexFiles.Count);

            CodeIndexBuilder.BuildIndexByBatch(Config, true, true, true, new[] { new FileInfo(Path.Combine(TempDir, "A.txt")), new FileInfo("BlaBla\\a.txt") }, false, null, out failedIndexFiles);
            Assert.AreEqual(0, failedIndexFiles.Count);
        }

        [Test]
        public void TestInitIndexFolderIfNeeded()
        {
            Assert.IsFalse(Directory.Exists(Config.LuceneIndexForCode));
            Assert.IsFalse(Directory.Exists(Config.LuceneIndexForHint));

            CodeIndexBuilder.InitIndexFolderIfNeeded(Config, null);

            Assert.IsTrue(Directory.Exists(Config.LuceneIndexForCode));
            Assert.IsTrue(Directory.Exists(Config.LuceneIndexForHint));
        }

        void BuildIndex()
        {
            CodeIndexBuilder.BuildIndex(Config, true, true, true, new[] { new CodeSource
            {
                FileName = "Dummy File",
                FileExtension = "cs",
                FilePath = @"C:\Dummy File.cs",
                Content = "Test Content" + Environment.NewLine + "A New Line For Test",
                IndexDate = new DateTime(2020, 1, 1)
            },
            new CodeSource
            {
                FileName = "A new File",
                FileExtension = "xml",
                FilePath = @"D:\DDDD\A new Name.cs",
                Content = "FFFF Content A new Line"
            }});
        }
    }
}
