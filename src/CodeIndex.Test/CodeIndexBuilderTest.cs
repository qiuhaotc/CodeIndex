using System;
using System.Linq;
using CodeIndex.Common;
using CodeIndex.IndexBuilder;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using NUnit.Framework;

namespace CodeIndex.Test
{
    public class CodeIndexBuilderTest : BaseTest
    {
        [Test]
        public void TestBuildIndex()
        {
            CodeIndexBuilder.BuildIndex(TempIndexDir, true, true, new CodeSource
                {
                    FileName = "Dummy File",
                    FileExtension = "cs",
                    FilePath = @"C:\Dummy File.cs",
                    Content = "Test Content" + Environment.NewLine + "A New Line For Test"
                },
                new CodeSource
                {
                    FileName = "A new File",
                    FileExtension = "xml",
                    FilePath = @"D:\DDDD\A new Name.cs",
                    Content = "FFFF Content A new Line"
                });

            CodeIndexBuilder.CloseIndexWriter(TempIndexDir);

            var result1 = CodeIndexSearcher.Search(TempIndexDir, new TermQuery(new Term(nameof(CodeSource.FileName), "Dummy File")), 10);
            Assert.That(result1.Length, Is.EqualTo(1));
            Assert.AreEqual(@"Dummy File", result1[0].Get(nameof(CodeSource.FileName)));
            Assert.AreEqual(@"cs", result1[0].Get(nameof(CodeSource.FileExtension)));
            Assert.AreEqual(@"C:\Dummy File.cs", result1[0].Get(nameof(CodeSource.FilePath)));
            Assert.AreEqual("Test Content" + Environment.NewLine + "A New Line For Test", result1[0].Get(nameof(CodeSource.Content)));

            var queryParser = new QueryParser(LuceneVersion.LUCENE_48, nameof(CodeSource.Content), new StandardAnalyzer(LuceneVersion.LUCENE_48));
            var result2 = CodeIndexSearcher.Search(TempIndexDir, queryParser.Parse("FFFF test"), 10);
            Assert.That(result2.Length, Is.EqualTo(2));
            Assert.IsTrue(result2.Any(u => u.Get(nameof(CodeSource.FileName)) == "Dummy File"));
            Assert.IsTrue(result2.Any(u => u.Get(nameof(CodeSource.FileName)) == "A new File"));

            var result3 = CodeIndexSearcher.Search(TempIndexDir, queryParser.Parse("FFFF"), 10);
            Assert.That(result3.Length, Is.EqualTo(1));
            Assert.IsTrue(result3.Any(u => u.Get(nameof(CodeSource.FileName)) == "A new File"));
        }
    }
}