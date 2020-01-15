using System;
using System.Linq;
using CodeIndex.Common;
using CodeIndex.IndexBuilder;
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
            CodeIndexBuilder.CloseIndexWriterAndCommitChange(TempIndexDir);

            var result1 = CodeIndexSearcher.Search(TempIndexDir, new TermQuery(new Term(nameof(CodeSource.FileName), "Dummy File")), 10);
            Assert.That(result1.Length, Is.EqualTo(1));
            Assert.AreEqual(@"Dummy File", result1[0].Get(nameof(CodeSource.FileName)));
            Assert.AreEqual(@"cs", result1[0].Get(nameof(CodeSource.FileExtension)));
            Assert.AreEqual(@"C:\Dummy File.cs", result1[0].Get(nameof(CodeSource.FilePath)));
            Assert.AreEqual("Test Content" + Environment.NewLine + "A New Line For Test", result1[0].Get(nameof(CodeSource.Content)));
            Assert.AreEqual(new DateTime(2020, 1, 1).Ticks, result1[0].GetField(nameof(CodeSource.IndexDate)).GetInt64Value());

            var queryParser = new QueryParser(CodeIndexBuilder.AppLuceneVersion, nameof(CodeSource.Content), new StandardAnalyzer(CodeIndexBuilder.AppLuceneVersion));
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
            CodeIndexBuilder.CloseIndexWriterAndCommitChange(TempIndexDir);

            var result = CodeIndexSearcher.Search(TempIndexDir, new TermQuery(new Term(nameof(CodeSource.FileName), "Dummy File")), 10);
            Assert.AreEqual("Test Content" + Environment.NewLine + "A New Line For Test", result.Single().Get(nameof(CodeSource.Content)));

            CodeIndexBuilder.BuildIndex(TempIndexDir, true, true, new CodeSource
            {
                FileName = "Dummy File New",
                FileExtension = "cs",
                FilePath = @"C:\Dummy File.cs",
                Content = "ABC",
                IndexDate = new DateTime(2020, 1, 1)
            });
            CodeIndexBuilder.CloseIndexWriterAndCommitChange(TempIndexDir);

            result = CodeIndexSearcher.Search(TempIndexDir, new TermQuery(new Term(nameof(CodeSource.FileName), "Dummy File New")), 10);
            Assert.AreEqual("ABC", result.Single().Get(nameof(CodeSource.Content)));
        }

        [Test]
        public void TestDeleteIndex()
        {
            BuildIndex();
            CodeIndexBuilder.CloseIndexWriterAndCommitChange(TempIndexDir);

            var queryParser = new QueryParser(CodeIndexBuilder.AppLuceneVersion, nameof(CodeSource.Content), new StandardAnalyzer(CodeIndexBuilder.AppLuceneVersion));
            var result = CodeIndexSearcher.Search(TempIndexDir, queryParser.Parse("FFFF test"), 10);
            Assert.That(result.Length, Is.EqualTo(2));

            CodeIndexBuilder.DeleteIndex(TempIndexDir, new Term(nameof(CodeSource.FileExtension), "xml"));
            CodeIndexBuilder.CloseIndexWriterAndCommitChange(TempIndexDir);
            result = CodeIndexSearcher.Search(TempIndexDir, queryParser.Parse("FFFF test"), 10);
            Assert.That(result.Length, Is.EqualTo(1));

            CodeIndexBuilder.DeleteIndex(TempIndexDir, queryParser.Parse("Test"));
            CodeIndexBuilder.CloseIndexWriterAndCommitChange(TempIndexDir);
            result = CodeIndexSearcher.Search(TempIndexDir, queryParser.Parse("FFFF test"), 10);
            Assert.That(result.Length, Is.EqualTo(0));
        }

        [Test]
        public void TestUpdateIndex()
        {
            BuildIndex();
            CodeIndexBuilder.CloseIndexWriterAndCommitChange(TempIndexDir);

            var result = CodeIndexSearcher.Search(TempIndexDir, new TermQuery(new Term(nameof(CodeSource.FilePath), @"D:\DDDD\A new Name.cs")), 10);
            Assert.That(result.Length, Is.EqualTo(1));

            CodeIndexBuilder.UpdateIndex(TempIndexDir, new Term(nameof(CodeSource.FilePath), @"d:\dddd\a new name.cs"), CodeIndexBuilder.GetDocumentFromSource(new CodeSource()
            {
                Content = "AAA",
                FileExtension = "CCC",
                FilePath = "BBB",
                FileName = "DDD",
                IndexDate = new DateTime(1999, 12, 31)
            }));
            CodeIndexBuilder.CloseIndexWriterAndCommitChange(TempIndexDir);

            result = CodeIndexSearcher.Search(TempIndexDir, new TermQuery(new Term(nameof(CodeSource.FilePath), @"d:\dddd\a new name.cs")), 10);
            Assert.That(result.Length, Is.EqualTo(0));

            result = CodeIndexSearcher.Search(TempIndexDir, new TermQuery(new Term(nameof(CodeSource.FilePath), "BBB")), 10);
            Assert.That(result.Length, Is.EqualTo(1));
            Assert.AreEqual(@"DDD", result[0].Get(nameof(CodeSource.FileName)));
            Assert.AreEqual(@"CCC", result[0].Get(nameof(CodeSource.FileExtension)));
            Assert.AreEqual(@"BBB", result[0].Get(nameof(CodeSource.FilePath)));
            Assert.AreEqual("AAA", result[0].Get(nameof(CodeSource.Content)));
            Assert.AreEqual(new DateTime(1999, 12, 31).Ticks, result[0].GetField(nameof(CodeSource.IndexDate)).GetInt64Value());
        }

        [Test]
        public void TestCreateOrGetIndexWriter()
        {
            BuildIndex();

            var index1 = CodeIndexBuilder.CreateOrGetIndexWriter(TempIndexDir);
            var index2 = CodeIndexBuilder.CreateOrGetIndexWriter(TempIndexDir);

            Assert.AreSame(index1, index2);
            Assert.AreSame(CodeIndexBuilder.IndexWritesPool[TempIndexDir], index1);
        }

        [Test]
        public void TestIndexExists()
        {
            Assert.IsFalse(CodeIndexBuilder.IndexExists(TempIndexDir));

            BuildIndex();

            Assert.IsFalse(CodeIndexBuilder.IndexExists(TempIndexDir));

            CodeIndexBuilder.CloseIndexWriterAndCommitChange(TempIndexDir);

            Assert.IsTrue(CodeIndexBuilder.IndexExists(TempIndexDir));
        }

        [Test]
        public void TestDeleteIndexFolder()
        {
            BuildIndex();
            CodeIndexBuilder.CloseIndexWriterAndCommitChange(TempIndexDir);
            Assert.IsTrue(CodeIndexBuilder.IndexExists(TempIndexDir));

            CodeIndexBuilder.DeleteAllIndex(TempIndexDir);
            Assert.IsFalse(CodeIndexBuilder.IndexExists(TempIndexDir));
        }

        void BuildIndex()
        {
            CodeIndexBuilder.BuildIndex(TempIndexDir, true, true, new CodeSource
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
            });
        }
    }
}
