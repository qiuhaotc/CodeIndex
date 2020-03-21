using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CodeIndex.Common;
using CodeIndex.IndexBuilder;
using Lucene.Net.Search;
using NUnit.Framework;

namespace CodeIndex.Test
{
    public class LucenePoolTest : BaseTest
    {
        [Test]
        public void TestGetQueryParserAndAnalyzer()
        {
            Assert.NotNull(LucenePool.GetQueryParser());
            Assert.NotNull(LucenePool.GetAnalyzer());
        }

        [Test]
        [Timeout(60000)]
        public void TestThreadSafeForIndexReader()
        {
            CodeIndexBuilder.BuildIndex(Config, true, true, true, new[] {
                new CodeSource
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
                }
            });

            LucenePool.SaveResultsAndClearLucenePool(Config);

            var taskList = new List<Task>();

            for (int i = 0; i < 20; i++)
            {
                taskList.Add(Task.Run(() =>
                {
                    for (int j = 0; j < 50; j++)
                    {
                        if (j % 4 == 0)
                        {
                            CodeIndexBuilder.BuildIndex(Config, true, true, true, new[] {
                                new CodeSource
                                {
                                    FileName = $"Dummy File 1 {i} {j}",
                                    FileExtension = "cs",
                                    FilePath = $@"C:\Dummy File 1 {i} {j}.cs",
                                    Content = "Test Content" + Environment.NewLine + "A New Line For Test"
                                }
                            });
                        }

                        LucenePool.Search(Config.LuceneIndexForCode, new MatchAllDocsQuery(), int.MaxValue);
                        LucenePool.Search(Config.LuceneIndexForCode, new MatchAllDocsQuery(), int.MaxValue);
                    }
                }));
            }

            Assert.DoesNotThrowAsync(async () => await Task.WhenAll(taskList));
        }

        [Test]
        public void TestGetHints()
        {
            WordsHintBuilder.Words.Add("ABCD");
            WordsHintBuilder.Words.Add("Abcdef");
            WordsHintBuilder.BuildIndexByBatch(Config, true, true, true, null, true);

            CollectionAssert.AreEquivalent(new[] { "ABCD" }, LucenePool.GetHints(Config.LuceneIndexForHint, "ABC", 10, true));
            CollectionAssert.AreEquivalent(new[] { "ABCD", "Abcdef" }, LucenePool.GetHints(Config.LuceneIndexForHint, "ABC", 10, false));
        }
    }
}
