using CodeIndex.Common;
using CodeIndex.IndexBuilder;
using CodeIndex.MaintainIndex;
using Lucene.Net.QueryParsers.Classic;
using NUnit.Framework;

namespace CodeIndex.Test
{
    public class QueryGeneratorTest
    {
        [Test]
        public void TestGetSearchQuery()
        {
            Assert.Multiple(() =>
            {
                Assert.IsEmpty(Generator.GetSearchStr(null, null, null, null));
                Assert.AreEqual($"{nameof(CodeSource.FileName)}:A", Generator.GetSearchStr("A", null, null, null));
                Assert.AreEqual($"{nameof(CodeSource.Content)}:B", Generator.GetSearchStr(null, "B", null, null));
                Assert.AreEqual($"{nameof(CodeSource.FileExtension)}:C", Generator.GetSearchStr(null, null, "C", null));
                Assert.AreEqual($"{nameof(CodeSource.FilePath)}:D", Generator.GetSearchStr(null, null, null, "D"));
                Assert.AreEqual($"{nameof(CodeSource.FileName)}:A AND {nameof(CodeSource.Content)}:B AND {nameof(CodeSource.FileExtension)}:C AND {nameof(CodeSource.FilePath)}:D", Generator.GetSearchStr("A", "B", "C", "D"));
                Assert.IsEmpty(Generator.GetSearchStr(" ", "   ", string.Empty, " "));
                Assert.AreEqual(nameof(CodeSource.FilePath) + ":" + "\"C:\\\\WWWROOT\"", Generator.GetSearchStr(null, null, null, "\"C:\\WWWROOT\""));
            });
        }

        QueryGeneratorForTest generator;
        QueryGeneratorForTest Generator => generator ??= new QueryGeneratorForTest(new QueryParser(Constants.AppLuceneVersion, nameof(CodeSource.Content), new CodeAnalyzer(Constants.AppLuceneVersion, true)));

        class QueryGeneratorForTest : QueryGenerator
        {
            public QueryGeneratorForTest(QueryParser queryParser) : base(queryParser)
            {
            }

            public new string GetSearchStr(string fileName, string content, string fileExtension, string filePath, bool caseSensitive = false, string codePk = null)
            {
                return base.GetSearchStr(fileName, content, fileExtension, filePath, caseSensitive, codePk);
            }
        }
    }
}
