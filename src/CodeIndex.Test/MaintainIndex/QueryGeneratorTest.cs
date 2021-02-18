using CodeIndex.Common;
using CodeIndex.IndexBuilder;
using CodeIndex.MaintainIndex;
using NUnit.Framework;

namespace CodeIndex.Test
{
    public class QueryGeneratorTest
    {
        [Test]
        public void TestGetSearchStr()
        {
            Assert.Multiple(() =>
            {
                Assert.IsEmpty(Generator.GetSearchStr(null, null, null));
                Assert.AreEqual($"{nameof(CodeSource.FileName)}:A", Generator.GetSearchStr("A", null, null));
                Assert.AreEqual($"{nameof(CodeSource.FileExtension)}:B", Generator.GetSearchStr(null, "B", null));
                Assert.AreEqual($"{nameof(CodeSource.FilePath)}:C", Generator.GetSearchStr(null, null, "C"));
                Assert.AreEqual($"{nameof(CodeSource.FileName)}:A AND {nameof(CodeSource.FileExtension)}:B AND {nameof(CodeSource.FilePath)}:C", Generator.GetSearchStr("A", "B", "C"));
                Assert.AreEqual($"{nameof(CodeSource.CodePK)}:D", Generator.GetSearchStr("A", "B", "C", "D"));
                Assert.IsEmpty(Generator.GetSearchStr(" ", "   ", string.Empty, " "));
                Assert.AreEqual(nameof(CodeSource.FilePath) + ":" + "\"C:\\\\WWWROOT\"", Generator.GetSearchStr(null, null, "\"C:\\WWWROOT\""));
            });
        }

        [Test]
        public void TestGetContentSearchStr()
        {
            Assert.Multiple(() =>
            {
                Assert.IsEmpty(Generator.GetContentSearchStr(null, false));
                Assert.IsEmpty(Generator.GetContentSearchStr(" ", true));
                Assert.AreEqual($"{nameof(CodeSource.Content)}:A", Generator.GetContentSearchStr("A", false));
                Assert.AreEqual($"{CodeIndexBuilder.GetCaseSensitiveField(nameof(CodeSource.Content))}:A", Generator.GetContentSearchStr("A", true));
            });
        }

        [Test]
        public void TestGetSearchQuery_NormalQuery()
        {
            var query = Generator.GetSearchQuery(new SearchRequest
            {
                CaseSensitive = false,
                Content = "ABC*",
                FileName = "EFG",
            });

            Assert.NotNull(query);
            Assert.AreEqual("+FileName:efg +Content:abc*", query.ToString());

            query = Generator.GetSearchQuery(new SearchRequest
            {
                CaseSensitive = true,
                Content = "ABC*",
                FileName = "EFG",
            });

            Assert.AreEqual($"+FileName:efg +{CodeIndexBuilder.GetCaseSensitiveField(nameof(CodeSource.Content))}:ABC*", query.ToString());

            query = Generator.GetSearchQuery(new SearchRequest
            {
                CaseSensitive = false,
                Content = "Abc~2",
                FileName = "EFG",
            });

            Assert.AreEqual($"+FileName:efg +Content:abc~2", query.ToString());

            query = Generator.GetSearchQuery(new SearchRequest
            {
                CaseSensitive = true,
                FileName = "EFG",
                Content = "Abc~2",
            });

            Assert.AreEqual($"+FileName:efg +{CodeIndexBuilder.GetCaseSensitiveField(nameof(CodeSource.Content))}:Abc~2", query.ToString());

            query = Generator.GetSearchQuery(new SearchRequest
            {
                CaseSensitive = true,
                FileName = "EFG",
            });

            Assert.AreEqual($"FileName:efg", query.ToString());

            query = Generator.GetSearchQuery(new SearchRequest
            {
                CaseSensitive = true,
                Content = "Abc~2",
            });

            Assert.AreEqual($"{CodeIndexBuilder.GetCaseSensitiveField(nameof(CodeSource.Content))}:Abc~2", query.ToString());

            query = Generator.GetSearchQuery(new SearchRequest
            {
                CaseSensitive = true,
                Content = "Abc~2",
                CodePK = "123"
            });

            Assert.AreEqual($"{nameof(CodeSource.CodePK)}:123", query.ToString());
        }

        [Test]
        public void TestGetSearchQuery_PhaseQuery()
        {
            var query = Generator.GetSearchQuery(new SearchRequest
            {
                PhaseQuery = true,
                Content = "ABC* DEF",
                FileName = "EFG",
            });

            Assert.NotNull(query);
            Assert.AreEqual("+SpanNear([SpanMultiTermQueryWrapper(Content:abc*), SpanMultiTermQueryWrapper(Content:def)], 0, True) +FileName:efg", query.ToString());

            query = Generator.GetSearchQuery(new SearchRequest
            {
                PhaseQuery = true,
                Content = "\"\\\"",
            });

            Assert.NotNull(query);
            Assert.AreEqual("+Content:\"\" \"\"", query.ToString());

            query = Generator.GetSearchQuery(new SearchRequest
            {
                PhaseQuery = true,
                Content = "ABC* DEF",
                FileName = "EFG",
                CodePK = "123"
            });

            Assert.AreEqual($"{nameof(CodeSource.CodePK)}:123", query.ToString());
        }

        [Test]
        public void TestGetContentSearchQuery_NormalQuery()
        {
            var query = Generator.GetContentSearchQuery(new SearchRequest
            {
                CaseSensitive = false,
                Content = "ABC*",
            });

            Assert.NotNull(query);
            Assert.AreEqual("Content:abc*", query.ToString());

            query = Generator.GetContentSearchQuery(new SearchRequest
            {
                CaseSensitive = true,
                Content = "ABC*",
            });

            Assert.AreEqual($"{CodeIndexBuilder.GetCaseSensitiveField(nameof(CodeSource.Content))}:ABC*", query.ToString());

            query = Generator.GetContentSearchQuery(new SearchRequest
            {
                CaseSensitive = false,
                Content = "Abc~2",
            });

            Assert.AreEqual($"Content:abc~2", query.ToString());

            query = Generator.GetContentSearchQuery(new SearchRequest
            {
                CaseSensitive = true,
                Content = "Abc~2",
            });

            Assert.AreEqual($"{CodeIndexBuilder.GetCaseSensitiveField(nameof(CodeSource.Content))}:Abc~2", query.ToString());

            query = Generator.GetContentSearchQuery(new SearchRequest
            {
                CaseSensitive = true,
                Content = "  ",
            });

            Assert.IsNull(query);

            query = Generator.GetContentSearchQuery(new SearchRequest
            {
                CaseSensitive = false,
                Content = null,
            });

            Assert.IsNull(query);
        }

        [Test]
        public void TestGetContentSearchQuery_PhaseQuery()
        {
            var query = Generator.GetSearchQuery(new SearchRequest
            {
                PhaseQuery = true,
                Content = "ABC* DEF",
            });

            Assert.NotNull(query);
            Assert.AreEqual("+SpanNear([SpanMultiTermQueryWrapper(Content:abc*), SpanMultiTermQueryWrapper(Content:def)], 0, True)", query.ToString());
        }

        QueryGeneratorForTest generator;
        QueryGeneratorForTest Generator => generator ??= new QueryGeneratorForTest();

        class QueryGeneratorForTest : QueryGenerator
        {
            public QueryGeneratorForTest() : base(LucenePoolLight.GetQueryParser(), LucenePoolLight.GetQueryParser(false))
            {
            }

            public new string GetSearchStr(string fileName, string fileExtension, string filePath, string codePk = null)
            {
                return base.GetSearchStr(fileName, fileExtension, filePath, codePk);
            }

            public new string GetContentSearchStr(string content, bool caseSensitive)
            {
                return base.GetContentSearchStr(content, caseSensitive);
            }
        }
    }
}
