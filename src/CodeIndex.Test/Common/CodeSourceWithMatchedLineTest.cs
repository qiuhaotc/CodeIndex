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
                Assert.That(codeWithMatchedLine.MatchedLine, Is.EqualTo(1));
                Assert.That(codeWithMatchedLine.MatchedContent, Is.EqualTo("ABC"));
                Assert.That(codeWithMatchedLine.CodeSource.Content, Is.EqualTo("ABD"));
            });

            Assert.DoesNotThrow(() => new CodeSourceWithMatchedLine());
        }
    }
}
