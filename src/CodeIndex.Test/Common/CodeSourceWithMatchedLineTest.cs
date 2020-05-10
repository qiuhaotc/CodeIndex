using CodeIndex.Common;
using Newtonsoft.Json;
using NUnit.Framework;

namespace CodeIndex.Test
{
    public class CodeSourceWithMatchedLineTest
    {
        [Test]
        public void TestConstructor()
        {
            var codeWithMatchedLine = new CodeSourceWithMatchedLine(new CodeSource() { Content = "ABD" }, 1, "ABC");
            Assert.Multiple(() =>
            {
                Assert.AreEqual(1, codeWithMatchedLine.MatchedLine);
                Assert.AreEqual("ABC", codeWithMatchedLine.MatchedContent);
                Assert.AreEqual("ABD", codeWithMatchedLine.CodeSource.Content);
            });

            Assert.DoesNotThrow(() => new CodeSourceWithMatchedLine());
        }
    }
}
