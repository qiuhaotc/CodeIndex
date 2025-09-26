using CodeIndex.Common;
using NUnit.Framework;

namespace CodeIndex.Test
{
    public class ExtendMethodsTest
    {
        [Test]
        public void TestSubStringSafe()
        {
            Assert.That("123".SubStringSafe(3, 4), Is.Empty);
            Assert.That("123".SubStringSafe(5, 1), Is.Empty);
            Assert.That("123".SubStringSafe(2, 4), Is.EqualTo("3"));
            Assert.That("123".SubStringSafe(0, 4), Is.EqualTo("123"));
            Assert.That("123".SubStringSafe(0, 2), Is.EqualTo("12"));
            Assert.That("123".SubStringSafe(-10, 2), Is.EqualTo("12"));
        }

        [Test]
        public void TestSubStringSafeWithEllipsis()
        {
            Assert.That("123".SubStringSafeWithEllipsis(3, 4), Is.EqualTo("..."));
            Assert.That("123".SubStringSafeWithEllipsis(5, 1, "###"), Is.EqualTo("###"));
            Assert.That("123".SubStringSafeWithEllipsis(2, 4), Is.EqualTo("...3"));
            Assert.That("123".SubStringSafeWithEllipsis(0, 4), Is.EqualTo("123"));
            Assert.That("123".SubStringSafeWithEllipsis(0, 2), Is.EqualTo("12..."));
            Assert.That("123".SubStringSafeWithEllipsis(-10, 2), Is.EqualTo("12..."));
        }
    }
}
