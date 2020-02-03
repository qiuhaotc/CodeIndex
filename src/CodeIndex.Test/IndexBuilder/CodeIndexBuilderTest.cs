using System;
using System.Linq;
using CodeIndex.Common;
using CodeIndex.IndexBuilder;
using CodeIndex.LuceneContainer;
using CodeIndex.Search;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers.Classic;
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
            LucenePool.SaveLuceneResultsAndCloseIndexWriter(TempIndexDir);

            var result1 = CodeIndexSearcher.Search(TempIndexDir, new TermQuery(new Term(nameof(CodeSource.FileName), "Dummy File")), 10);
            Assert.That(result1.Length, Is.EqualTo(1));
            Assert.AreEqual(@"Dummy File", result1[0].Get(nameof(CodeSource.FileName)));
            Assert.AreEqual(@"cs", result1[0].Get(nameof(CodeSource.FileExtension)));
            Assert.AreEqual(@"C:\Dummy File.cs", result1[0].Get(nameof(CodeSource.FilePath)));
            Assert.AreEqual("Test Content" + Environment.NewLine + "A New Line For Test", result1[0].Get(nameof(CodeSource.Content)));
            Assert.AreEqual(new DateTime(2020, 1, 1).Ticks, result1[0].GetField(nameof(CodeSource.IndexDate)).GetInt64Value());

            var queryParser = new QueryParser(Constants.AppLuceneVersion, nameof(CodeSource.Content), new StandardAnalyzer(Constants.AppLuceneVersion));
            var result2 = CodeIndexSearcher.Search(TempIndexDir, queryParser.Parse("FFFF test"), 10);
            Assert.That(result2.Length, Is.EqualTo(2));
            Assert.IsTrue(result2.Any(u => u.Get(nameof(CodeSource.FileName)) == "Dummy File"));
            Assert.IsTrue(result2.Any(u => u.Get(nameof(CodeSource.FileName)) == "A new File"));

            var result3 = CodeIndexSearcher.Search(TempIndexDir, queryParser.Parse("FFFF"), 10);
            Assert.That(result3.Length, Is.EqualTo(1));
            Assert.IsTrue(result3.Any(u => u.Get(nameof(CodeSource.FileName)) == "A new File"));
        }

        [Test]
        public void TestBuildIndex_DeleteOldIndexWithSamePath()
        {
            BuildIndex();
            LucenePool.SaveLuceneResultsAndCloseIndexWriter(TempIndexDir);

            var result = CodeIndexSearcher.Search(TempIndexDir, new TermQuery(new Term(nameof(CodeSource.FileName), "Dummy File")), 10);
            Assert.AreEqual("Test Content" + Environment.NewLine + "A New Line For Test", result.Single().Get(nameof(CodeSource.Content)));

            CodeIndexBuilder.BuildIndex(TempIndexDir, true, true, true, new[] { new CodeSource
            {
                FileName = "Dummy File New",
                FileExtension = "cs",
                FilePath = @"C:\Dummy File.cs",
                Content = "ABC",
                IndexDate = new DateTime(2020, 1, 1)
            }});
            LucenePool.SaveLuceneResultsAndCloseIndexWriter(TempIndexDir);

            result = CodeIndexSearcher.Search(TempIndexDir, new TermQuery(new Term(nameof(CodeSource.FileName), "Dummy File New")), 10);
            Assert.AreEqual("ABC", result.Single().Get(nameof(CodeSource.Content)));
        }

        [Test]
        public void TestDeleteIndex()
        {
            BuildIndex();
            LucenePool.SaveLuceneResultsAndCloseIndexWriter(TempIndexDir);

            var queryParser = new QueryParser(Constants.AppLuceneVersion, nameof(CodeSource.Content), new StandardAnalyzer(Constants.AppLuceneVersion));
            var result = CodeIndexSearcher.Search(TempIndexDir, queryParser.Parse("FFFF test"), 10);
            Assert.That(result.Length, Is.EqualTo(2));

            CodeIndexBuilder.DeleteIndex(TempIndexDir, new Term(nameof(CodeSource.FileExtension), "xml"));
            LucenePool.SaveLuceneResultsAndCloseIndexWriter(TempIndexDir);
            result = CodeIndexSearcher.Search(TempIndexDir, queryParser.Parse("FFFF test"), 10);
            Assert.That(result.Length, Is.EqualTo(1));

            CodeIndexBuilder.DeleteIndex(TempIndexDir, queryParser.Parse("Test"));
            LucenePool.SaveLuceneResultsAndCloseIndexWriter(TempIndexDir);
            result = CodeIndexSearcher.Search(TempIndexDir, queryParser.Parse("FFFF test"), 10);
            Assert.That(result.Length, Is.EqualTo(0));
        }

        [Test]
        public void TestUpdateIndex()
        {
            BuildIndex();
            LucenePool.SaveLuceneResultsAndCloseIndexWriter(TempIndexDir);

            var result = CodeIndexSearcher.Search(TempIndexDir, new TermQuery(new Term(nameof(CodeSource.FilePath), @"D:\DDDD\A new Name.cs")), 10);
            Assert.That(result.Length, Is.EqualTo(1));

            CodeIndexBuilder.UpdateIndex(TempIndexDir, new Term(nameof(CodeSource.FilePath), @"d:\dddd\a new name.cs"), CodeIndexBuilder.GetDocumentFromSource(new CodeSource()
            {
                Content = "AAA",
                FileExtension = "CCC",
                FilePath = "BBB",
                FileName = "DDD",
                IndexDate = new DateTime(1999, 12, 31),
                LastWriteTimeUtc = new DateTime(2000, 1, 1)
            }));
            LucenePool.SaveLuceneResultsAndCloseIndexWriter(TempIndexDir);

            result = CodeIndexSearcher.Search(TempIndexDir, new TermQuery(new Term(nameof(CodeSource.FilePath), @"d:\dddd\a new name.cs")), 10);
            Assert.That(result.Length, Is.EqualTo(0));

            result = CodeIndexSearcher.Search(TempIndexDir, new TermQuery(new Term(nameof(CodeSource.FilePath), "BBB")), 10);
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
            Assert.IsFalse(CodeIndexBuilder.IndexExists(TempIndexDir));

            BuildIndex();

            Assert.IsFalse(CodeIndexBuilder.IndexExists(TempIndexDir));

            LucenePool.SaveLuceneResultsAndCloseIndexWriter(TempIndexDir);

            Assert.IsTrue(CodeIndexBuilder.IndexExists(TempIndexDir));
        }

        [Test]
        public void TestDeleteIndexFolder()
        {
            BuildIndex();
            LucenePool.SaveLuceneResultsAndCloseIndexWriter(TempIndexDir);
            Assert.AreEqual(1, CodeIndexSearcher.Search(TempIndexDir, new TermQuery(new Term(nameof(CodeSource.FileName), "Dummy File")), 10).Length);
            Assert.AreEqual(1, CodeIndexSearcher.Search(TempIndexDir, new TermQuery(new Term(nameof(CodeSource.FileName), "A new File")), 10).Length);

            CodeIndexBuilder.DeleteAllIndex(TempIndexDir);
            Assert.AreEqual(0, CodeIndexSearcher.Search(TempIndexDir, new TermQuery(new Term(nameof(CodeSource.FileName), "Dummy File")), 10).Length);
            Assert.AreEqual(0, CodeIndexSearcher.Search(TempIndexDir, new TermQuery(new Term(nameof(CodeSource.FileName), "A new File")), 10).Length);
        }

        void BuildIndex()
        {
            CodeIndexBuilder.BuildIndex(TempIndexDir, true, true, true, new[] { new CodeSource
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