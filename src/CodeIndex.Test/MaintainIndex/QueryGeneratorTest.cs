using CodeIndex.Common;
using CodeIndex.IndexBuilder;
using CodeIndex.MaintainIndex;
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
                Assert.IsEmpty(QueryGenerator.GetSearchStr(null, null, null, null));
                Assert.AreEqual($"{nameof(CodeSource.FileName)}:A", QueryGenerator.GetSearchStr("A", null, null, null));
                Assert.AreEqual($"{nameof(CodeSource.Content)}:B", QueryGenerator.GetSearchStr(null, "B", null, null));
                Assert.AreEqual($"{nameof(CodeSource.FileExtension)}:C", QueryGenerator.GetSearchStr(null, null, "C", null));
                Assert.AreEqual($"{nameof(CodeSource.FilePath)}:D", QueryGenerator.GetSearchStr(null, null, null, "D"));
                Assert.AreEqual($"{nameof(CodeSource.FileName)}:A AND {nameof(CodeSource.Content)}:B AND {nameof(CodeSource.FileExtension)}:C AND {nameof(CodeSource.FilePath)}:D", QueryGenerator.GetSearchStr("A", "B", "C", "D"));
                Assert.IsEmpty(QueryGenerator.GetSearchStr(" ", "   ", string.Empty, " "));
                Assert.AreEqual(nameof(CodeSource.FilePath) + ":" + "\"C:\\\\WWWROOT\"", QueryGenerator.GetSearchStr(null, null, null, "\"C:\\WWWROOT\""));
            });
        }
    }
}
