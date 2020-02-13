using System;
using CodeIndex.Common;
using CodeIndex.IndexBuilder;
using CodeIndex.Search;
using Lucene.Net.Index;
using Lucene.Net.Search;
using NUnit.Framework;

namespace CodeIndex.Test
{
    public class CodeIndexSearcherTest : BaseTest
    {
        [Test]
        public void TestSearch_NewReader()
        {
            CodeIndexBuilder.BuildIndex(TempIndexDir, true, true, true, new[] { new CodeSource
            {
                FileName = "Dummy File 1",
                FileExtension = "cs",
                FilePath = @"C:\Dummy File 1.cs",
                Content = "Test Content" + Environment.NewLine + "A New Line For Test"
            },
            new CodeSource
            {
                FileName = "Dummy File 2",
                FileExtension = "cs",
                FilePath = @"C:\Dummy File 2.cs",
                Content = "Test Content" + Environment.NewLine + "A New Line For Test"
            },
            new CodeSource
            {
                FileName = "Dummy File 2",
                FileExtension = "xml",
                FilePath = @"C:\Dummy File.xml",
                Content = "Test Content" + Environment.NewLine + "A New Line For Test"
            }});

            LucenePool.SaveResultsAndClearLucenePool(TempIndexDir);

            var results1 = CodeIndexSearcher.Search(TempIndexDir, new TermQuery(new Term(nameof(CodeSource.FileExtension), "cs")), 10);
            Assert.That(results1.Length, Is.EqualTo(2));

            var results2 = CodeIndexSearcher.Search(TempIndexDir, new TermQuery(new Term(nameof(CodeSource.FileExtension), "cs")), 1);
            Assert.That(results2.Length, Is.EqualTo(1));
        }

        [Test]
        public void TestSearch_ReaderFromWriter()
        {
            CodeIndexBuilder.BuildIndex(TempIndexDir, true, true, true, new[] { new CodeSource
            {
                FileName = "Dummy File 1",
                FileExtension = "cs",
                FilePath = @"C:\Dummy File 1.cs",
                Content = "Test Content" + Environment.NewLine + "A New Line For Test"
            },
            new CodeSource
            {
                FileName = "Dummy File 2",
                FileExtension = "cs",
                FilePath = @"C:\Dummy File 2.cs",
                Content = "Test Content" + Environment.NewLine + "A New Line For Test"
            },
            new CodeSource
            {
                FileName = "Dummy File 2",
                FileExtension = "xml",
                FilePath = @"C:\Dummy File.xml",
                Content = "Test Content" + Environment.NewLine + "A New Line For Test"
            }});

            var reader = LucenePool.IndexWritesPool[TempIndexDir].GetReader(true);
            var results = CodeIndexSearcher.Search(TempIndexDir, reader, new TermQuery(new Term(nameof(CodeSource.FileExtension), "xml")), 10);
            Assert.That(results.Length, Is.EqualTo(1));
            reader.Dispose();
        }

        [Test]
        public void TestGeneratePreviewText()
        {
            var generator = new QueryGenerator();
            var content = "My ABC\r\nIs A ABC CONTENT\r\nIt's abc in lowercase\r\nIt's Abc in mix\r\nNot AB with C";
            var result = CodeIndexSearcher.GeneratePreviewText(generator.GetQueryFromStr("ABC"), content, int.MaxValue, LucenePool.GetAnalyzer());
            Assert.AreEqual(@"My <label class='highlight'>ABC</label>
Is A <label class='highlight'>ABC</label> CONTENT
It's <label class='highlight'>abc</label> in lowercase
It's <label class='highlight'>Abc</label> in mix
Not AB with C", result);

            result = CodeIndexSearcher.GeneratePreviewText(generator.GetQueryFromStr("ABC"), content, 10, LucenePool.GetAnalyzer());
            Assert.AreEqual(@"My <label class='highlight'>ABC</label>
Is A <label class='highlight'>ABC</label>...
It's <label class='highlight'>Abc</label>", result);
        }
    }
}
