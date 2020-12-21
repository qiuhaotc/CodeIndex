using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CodeIndex.Common;
using CodeIndex.IndexBuilder;
using Lucene.Net.Analysis.Miscellaneous;
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
        public void TestSearchWithSpecificFields()
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

            var documents = light.SearchWithSpecificFields(new TermQuery(new Term(nameof(CodeSource.FileName), "2")), int.MaxValue, nameof(CodeSource.FileName), nameof(CodeSource.FileExtension));
            Assert.AreEqual(1, documents.Length);
            Assert.AreEqual("Dummy File 2", documents[0].Get(nameof(CodeSource.FileName)));
            Assert.AreEqual("cs", documents[0].Get(nameof(CodeSource.FileExtension)));
            Assert.IsNull(documents[0].Get(nameof(CodeSource.Content)));

            documents = light.SearchWithSpecificFields(new TermQuery(new Term(nameof(CodeSource.FileName), "2")), int.MaxValue, nameof(CodeSource.Content));
            Assert.AreEqual(1, documents.Length);
            Assert.IsNull(documents[0].Get(nameof(CodeSource.FileName)));
            Assert.IsNull(documents[0].Get(nameof(CodeSource.FileExtension)));
            Assert.AreEqual("Test Content" + Environment.NewLine + "A New Line For Test", documents[0].Get(nameof(CodeSource.Content)));

            documents = light.SearchWithSpecificFields(new MatchAllDocsQuery(), int.MaxValue, nameof(CodeSource.FileName));
            Assert.AreEqual(2, documents.Length);
            CollectionAssert.AreEquivalent(new[] { "Dummy File 1", "Dummy File 2" }, documents.Select(u => u.Get(nameof(CodeSource.FileName))));
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
        public void TestDeleteIndexWithDocumentsBeenDeleted()
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

            light.DeleteIndex(new Term(nameof(CodeSource.FileName), "2"), out documents);
            Assert.AreEqual(1, documents.Length);
            Assert.AreEqual("Dummy File 2", documents[0].Get(nameof(CodeSource.FileName)));

            documents = light.Search(new MatchAllDocsQuery(), int.MaxValue);
            Assert.AreEqual(1, documents.Length);

            light.DeleteIndex(new Term(nameof(CodeSource.FileName), "1"), out documents);
            Assert.AreEqual(1, documents.Length);
            Assert.AreEqual("Dummy File 1", documents[0].Get(nameof(CodeSource.FileName)));

            documents = light.Search(new MatchAllDocsQuery(), int.MaxValue);
            Assert.AreEqual(0, documents.Length);

            light.DeleteIndex(new Term(nameof(CodeSource.FileName), "1"), out documents);
            Assert.AreEqual(0, documents.Length);
        }

        [Test]
        public void TestAnalyzer()
        {
            Assert.NotNull(LucenePoolLight.Analyzer);
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
        public void TestUpdateIndexCaseSensitive()
        {
            using var light = new LucenePoolLight(TempIndexDir);

            var wordA = "ABC";
            light.UpdateIndex(new Term(nameof(CodeWord.Word), wordA), new Document
            {
                new StringField(nameof(CodeWord.Word), wordA, Field.Store.YES),
                new StringField(nameof(CodeWord.WordLower), wordA.ToLowerInvariant(), Field.Store.YES)
            });

            var documents = light.Search(new MatchAllDocsQuery(), int.MaxValue);
            Assert.AreEqual(1, documents.Length);

            var wordB = "Abc";
            light.UpdateIndex(new Term(nameof(CodeWord.Word), wordB), new Document
            {
                new StringField(nameof(CodeWord.Word), wordB, Field.Store.YES),
                new StringField(nameof(CodeWord.WordLower), wordB.ToLowerInvariant(), Field.Store.YES)
            });

            documents = light.Search(new MatchAllDocsQuery(), int.MaxValue);
            Assert.AreEqual(2, documents.Length);

            light.UpdateIndex(new Term(nameof(CodeWord.Word), wordB), new Document
            {
                new StringField(nameof(CodeWord.Word), wordB, Field.Store.YES),
                new StringField(nameof(CodeWord.WordLower), wordB.ToLowerInvariant(), Field.Store.YES)
            });

            documents = light.Search(new MatchAllDocsQuery(), int.MaxValue);
            Assert.AreEqual(2, documents.Length);
            CollectionAssert.AreEquivalent(new[] { "ABC", "Abc" }, documents.Select(u => u.Get(nameof(CodeWord.Word))));
        }

        [Test]
        public void TestUpdateIndexWithRawDocuments()
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

            light.UpdateIndex(new Term(nameof(CodeSource.FileName), "1"),
                GetDocument(new CodeSource
                {
                    FileName = "Dummy File New 1",
                    FileExtension = "cs",
                    FilePath = @"C:\Dummy File 2.cs",
                    Content = "New Content"
                }), out var rawDocuments);

            Assert.AreEqual(1, rawDocuments.Length);
            Assert.AreEqual("Test Content" + Environment.NewLine + "A New Line For Test", rawDocuments[0].Get(nameof(CodeSource.Content)), "Still old value");

            var documents = light.Search(new TermQuery(new Term(nameof(CodeSource.FileName), "1")), 10);
            Assert.AreEqual(1, documents.Length);
            Assert.AreEqual("New Content", documents[0].Get(nameof(CodeSource.Content)), "Content updated");
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

        [Test]
        public void TestGetQueryParser()
        {
            using var light = new LucenePoolLight(TempIndexDir);

            var parserA = LucenePoolLight.GetQueryParser();
            var parserB = LucenePoolLight.GetQueryParser();
            Assert.AreNotEqual(parserA, parserB);
            Assert.AreEqual(parserA.Analyzer, parserB.Analyzer);
            Assert.IsTrue(parserA.Analyzer is PerFieldAnalyzerWrapper);
        }

        [Test]
        public void TestExists()
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

            Assert.IsTrue(light.Exists(new TermQuery(new Term(nameof(CodeSource.FileName), "1"))));
            Assert.IsTrue(light.Exists(new TermQuery(new Term(nameof(CodeSource.FileName), "2"))));
            Assert.IsFalse(light.Exists(new TermQuery(new Term(nameof(CodeSource.FileName), "3"))));
        }

        Document GetDocument(CodeSource codeSource)
        {
            return IndexBuilderHelper.GetDocumentFromSource(codeSource);
        }
    }
}
