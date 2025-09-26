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

            Assert.That(word.Word, Is.EqualTo("ABc"));
            Assert.That(word.WordLower, Is.EqualTo("abc"));
        }
    }
}
