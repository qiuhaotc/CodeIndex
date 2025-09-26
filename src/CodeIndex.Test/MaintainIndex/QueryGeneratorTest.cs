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
                Assert.That(Generator.GetSearchStr(null, null, null), Is.Empty);
                Assert.That(Generator.GetSearchStr("A", null, null), Is.EqualTo($"{nameof(CodeSource.FileName)}:A"));
                Assert.That(Generator.GetSearchStr(null, "B", null), Is.EqualTo($"{nameof(CodeSource.FileExtension)}:B"));
                Assert.That(Generator.GetSearchStr(null, null, "C"), Is.EqualTo($"{nameof(CodeSource.FilePath)}:C"));
                Assert.That(Generator.GetSearchStr("A", "B", "C"), Is.EqualTo($"{nameof(CodeSource.FileName)}:A AND {nameof(CodeSource.FileExtension)}:B AND {nameof(CodeSource.FilePath)}:C"));
                Assert.That(Generator.GetSearchStr("A", "B", "C", "D"), Is.EqualTo($"{nameof(CodeSource.CodePK)}:D"));
                Assert.That(Generator.GetSearchStr(" ", "   ", string.Empty, " "), Is.Empty);
                Assert.That(Generator.GetSearchStr(null, null, "\"C:\\WWWROOT\""), Is.EqualTo(nameof(CodeSource.FilePath) + ":" + "\"C:\\\\WWWROOT\""));
            });
        }

        [Test]
        public void TestGetContentSearchStr()
        {
            Assert.Multiple(() =>
            {
                Assert.That(Generator.GetContentSearchStr(null, false), Is.Empty);
                Assert.That(Generator.GetContentSearchStr(" ", true), Is.Empty);
                Assert.That(Generator.GetContentSearchStr("A", false), Is.EqualTo($"{nameof(CodeSource.Content)}:A"));
                Assert.That(Generator.GetContentSearchStr("A", true), Is.EqualTo($"{CodeIndexBuilder.GetCaseSensitiveField(nameof(CodeSource.Content))}:A"));
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

            Assert.That(query, Is.Not.Null);
            Assert.That(query.ToString(), Is.EqualTo("+FileName:efg +Content:abc*"));

            query = Generator.GetSearchQuery(new SearchRequest
            {
                CaseSensitive = true,
                Content = "ABC*",
                FileName = "EFG",
            });

            Assert.That(query.ToString(), Is.EqualTo($"+FileName:efg +{CodeIndexBuilder.GetCaseSensitiveField(nameof(CodeSource.Content))}:ABC*"));

            query = Generator.GetSearchQuery(new SearchRequest
            {
                CaseSensitive = false,
                Content = "Abc~2",
                FileName = "EFG",
            });

            Assert.That(query.ToString(), Is.EqualTo($"+FileName:efg +Content:abc~2"));

            query = Generator.GetSearchQuery(new SearchRequest
            {
                CaseSensitive = true,
                FileName = "EFG",
                Content = "Abc~2",
            });

            Assert.That(query.ToString(), Is.EqualTo($"+FileName:efg +{CodeIndexBuilder.GetCaseSensitiveField(nameof(CodeSource.Content))}:Abc~2"));

            query = Generator.GetSearchQuery(new SearchRequest
            {
                CaseSensitive = true,
                FileName = "EFG",
            });

            Assert.That(query.ToString(), Is.EqualTo($"FileName:efg"));

            query = Generator.GetSearchQuery(new SearchRequest
            {
                CaseSensitive = true,
                Content = "Abc~2",
            });

            Assert.That(query.ToString(), Is.EqualTo($"{CodeIndexBuilder.GetCaseSensitiveField(nameof(CodeSource.Content))}:Abc~2"));

            query = Generator.GetSearchQuery(new SearchRequest
            {
                CaseSensitive = true,
                Content = "Abc~2",
                CodePK = "123"
            });

            Assert.That(query.ToString(), Is.EqualTo($"{nameof(CodeSource.CodePK)}:123"));
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

            Assert.That(query, Is.Not.Null);
            Assert.That(query.ToString(), Is.EqualTo("+SpanNear([SpanMultiTermQueryWrapper(Content:abc*), SpanMultiTermQueryWrapper(Content:def)], 0, True) +FileName:efg"));

            query = Generator.GetSearchQuery(new SearchRequest
            {
                PhaseQuery = true,
                Content = "\"\\\"",
            });

            Assert.That(query, Is.Not.Null);
            Assert.That(query.ToString(), Is.EqualTo("+Content:\"\" \"\""));

            query = Generator.GetSearchQuery(new SearchRequest
            {
                PhaseQuery = true,
                Content = "ABC* DEF",
                FileName = "EFG",
                CodePK = "123"
            });

            Assert.That(query.ToString(), Is.EqualTo($"{nameof(CodeSource.CodePK)}:123"));
        }

        [Test]
        public void TestGetContentSearchQuery_NormalQuery()
        {
            var query = Generator.GetContentSearchQuery(new SearchRequest
            {
                CaseSensitive = false,
                Content = "ABC*",
            });

            Assert.That(query, Is.Not.Null);
            Assert.That(query.ToString(), Is.EqualTo("Content:abc*"));

            query = Generator.GetContentSearchQuery(new SearchRequest
            {
                CaseSensitive = true,
                Content = "ABC*",
            });

            Assert.That(query.ToString(), Is.EqualTo($"{CodeIndexBuilder.GetCaseSensitiveField(nameof(CodeSource.Content))}:ABC*"));

            query = Generator.GetContentSearchQuery(new SearchRequest
            {
                CaseSensitive = false,
                Content = "Abc~2",
            });

            Assert.That(query.ToString(), Is.EqualTo($"Content:abc~2"));

            query = Generator.GetContentSearchQuery(new SearchRequest
            {
                CaseSensitive = true,
                Content = "Abc~2",
            });

            Assert.That(query.ToString(), Is.EqualTo($"{CodeIndexBuilder.GetCaseSensitiveField(nameof(CodeSource.Content))}:Abc~2"));

            query = Generator.GetContentSearchQuery(new SearchRequest
            {
                CaseSensitive = true,
                Content = "  ",
            });

            Assert.That(query, Is.Null);

            query = Generator.GetContentSearchQuery(new SearchRequest
            {
                CaseSensitive = false,
                Content = null,
            });

            Assert.That(query, Is.Null);
        }

        [Test]
        public void TestGetContentSearchQuery_PhaseQuery()
        {
            var query = Generator.GetSearchQuery(new SearchRequest
            {
                PhaseQuery = true,
                Content = "ABC* DEF",
            });

            Assert.That(query, Is.Not.Null);
            Assert.That(query.ToString(), Is.EqualTo("+SpanNear([SpanMultiTermQueryWrapper(Content:abc*), SpanMultiTermQueryWrapper(Content:def)], 0, True)"));
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
