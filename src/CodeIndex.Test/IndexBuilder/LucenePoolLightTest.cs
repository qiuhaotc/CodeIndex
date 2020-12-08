using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CodeIndex.Common;
using CodeIndex.IndexBuilder;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using NUnit.Framework;

namespace CodeIndex.Test
{
    public class LucenePoolLightTest : BaseTestLight
    {
        [Test]
        public void TestSearch()
        {
            using var light = new LucenePoolLight(TempIndexDir);

            light.BuildIndex(new[] {
                GetDocument(new CodeSource
                {
                    FileName = "Dummy File 1",
                    FileExtension = "cs",
                    FilePath = @"C:\Dummy File 1.cs",
                    Content = "Test Content" + Environment.NewLine + "A New Line For Test"
                }),
                GetDocument(new CodeSource
                {
                    FileName = "Dummy File 2",
                    FileExtension = "cs",
                    FilePath = @"C:\Dummy File 2.cs",
                    Content = "Test Content" + Environment.NewLine + "A New Line For Test"
                })}, true, true, true);

            var documents = light.Search(new MatchAllDocsQuery(), int.MaxValue);
            Assert.AreEqual(2, documents.Length);

            documents = light.Search(new TermQuery(new Term(nameof(CodeSource.FileName), "2")), int.MaxValue);
            Assert.AreEqual(1, documents.Length);
        }

        [Test]
        public void TestDeleteIndex()
        {
            using var light = new LucenePoolLight(TempIndexDir);

            light.BuildIndex(new[] {
                GetDocument(new CodeSource
                {
                    FileName = "Dummy File 1",
                    FileExtension = "cs",
                    FilePath = @"C:\Dummy File 1.cs",
                    Content = "Test Content" + Environment.NewLine + "A New Line For Test"
                }),
                GetDocument(new CodeSource
                {
                    FileName = "Dummy File 2",
                    FileExtension = "cs",
                    FilePath = @"C:\Dummy File 2.cs",
                    Content = "Test Content" + Environment.NewLine + "A New Line For Test"
                })}, true, true, true);

            var documents = light.Search(new MatchAllDocsQuery(), int.MaxValue);
            Assert.AreEqual(2, documents.Length);

            light.DeleteIndex(new TermQuery(new Term(nameof(CodeSource.FileName), "2")));
            documents = light.Search(new MatchAllDocsQuery(), int.MaxValue);
            Assert.AreEqual(1, documents.Length);

            light.DeleteIndex(new Term(nameof(CodeSource.FileName), "1"));
            documents = light.Search(new MatchAllDocsQuery(), int.MaxValue);
            Assert.AreEqual(0, documents.Length);
        }

        [Test]
        public void TestAnalyzer()
        {
            using var light = new LucenePoolLight("Dummy");
            Assert.NotNull(light.Analyzer);
        }

        [Test]
        public void TestDeleteAllIndex()
        {
            using var light = new LucenePoolLight(TempIndexDir);

            light.BuildIndex(new[] {
                GetDocument(new CodeSource
                {
                    FileName = "Dummy File 1",
                    FileExtension = "cs",
                    FilePath = @"C:\Dummy File 1.cs",
                    Content = "Test Content" + Environment.NewLine + "A New Line For Test"
                }),
                GetDocument(new CodeSource
                {
                    FileName = "Dummy File 2",
                    FileExtension = "cs",
                    FilePath = @"C:\Dummy File 2.cs",
                    Content = "Test Content" + Environment.NewLine + "A New Line For Test"
                })}, true, true, true);

            var documents = light.Search(new MatchAllDocsQuery(), int.MaxValue);
            Assert.AreEqual(2, documents.Length);

            light.DeleteAllIndex();
            documents = light.Search(new MatchAllDocsQuery(), int.MaxValue);
            Assert.AreEqual(0, documents.Length);
        }

        [Test]
        [Timeout(60000)]
        public void TestThreadSafeForIndexReader()
        {
            using var light = new LucenePoolLight(TempIndexDir);

            light.BuildIndex(new[] {
                GetDocument(new CodeSource
                {
                    FileName = "Dummy File 1",
                    FileExtension = "cs",
                    FilePath = @"C:\Dummy File 1.cs",
                    Content = "Test Content" + Environment.NewLine + "A New Line For Test"
                }),
                GetDocument(new CodeSource
                {
                    FileName = "Dummy File 2",
                    FileExtension = "cs",
                    FilePath = @"C:\Dummy File 2.cs",
                    Content = "Test Content" + Environment.NewLine + "A New Line For Test"
                }),
                GetDocument(new CodeSource
                {
                    FileName = "Dummy File 2",
                    FileExtension = "xml",
                    FilePath = @"C:\Dummy File.xml",
                    Content = "Test Content" + Environment.NewLine + "A New Line For Test"
                })
            }, true, true, true);

            var taskList = new List<Task>();
            var taskNumber = 3; // set to larger one for real test
            var addDocumentCount = 10; // set to larger one for real test

            for (var i = 0; i < taskNumber; i++)
            {
                taskList.Add(Task.Run(() =>
                {
                    for (var j = 0; j < addDocumentCount; j++)
                    {
                        if (j % 4 == 0)
                        {
                            light.BuildIndex(new[] {
                                GetDocument(new CodeSource
                                {
                                    FileName = $"Dummy File 1 {i} {j}",
                                    FileExtension = "cs",
                                    FilePath = $@"C:\Dummy File 1 {i} {j}.cs",
                                    Content = "Test Content" + Environment.NewLine + "A New Line For Test"
                                })
                            }, true, true, true);
                        }

                        light.Search(new MatchAllDocsQuery(), int.MaxValue);
                        light.Search(new MatchAllDocsQuery(), int.MaxValue);
                    }
                }));
            }

            Assert.DoesNotThrowAsync(async () => await Task.WhenAll(taskList));
        }

        Document GetDocument(CodeSource codeSource)
        {
            return CodeIndexBuilder.GetDocumentFromSource(codeSource);
        }
    }
}
