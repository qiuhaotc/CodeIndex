using CodeIndex.Common;
using NUnit.Framework;

namespace CodeIndex.Test
{
    public class ExtendMethodsTest
    {
        [Test]
        public void TestSubStringSafe()
        {
            Assert.IsEmpty("123".SubStringSafe(3, 4));
            Assert.IsEmpty("123".SubStringSafe(5, 1));
            Assert.AreEqual("3", "123".SubStringSafe(2, 4));
            Assert.AreEqual("123", "123".SubStringSafe(0, 4));
            Assert.AreEqual("12", "123".SubStringSafe(0, 2));
            Assert.AreEqual("12", "123".SubStringSafe(-10, 2));
        }

        [Test]
        public void TestSubStringSafeWithEllipsis()
        {
            Assert.AreEqual("...", "123".SubStringSafeWithEllipsis(3, 4));
            Assert.AreEqual("###", "123".SubStringSafeWithEllipsis(5, 1, "###"));
            Assert.AreEqual("...3", "123".SubStringSafeWithEllipsis(2, 4));
            Assert.AreEqual("123", "123".SubStringSafeWithEllipsis(0, 4));
            Assert.AreEqual("12...", "123".SubStringSafeWithEllipsis(0, 2));
            Assert.AreEqual("12...", "123".SubStringSafeWithEllipsis(-10, 2));
        }
    }
}
