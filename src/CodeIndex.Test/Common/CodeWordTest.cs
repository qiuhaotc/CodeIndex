using CodeIndex.Common;
using NUnit.Framework;

namespace CodeIndex.Test
{
    public class CodeWordTest
    {
        [Test]
        public void TestProperties()
        {
            var word = new CodeWord
            {
                Word = "ABc",
                WordLower = "abc"
            };

            Assert.AreEqual("ABc", word.Word);
            Assert.AreEqual("abc", word.WordLower);
        }
    }
}
