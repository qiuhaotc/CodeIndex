using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using CodeIndex.Common;
using NUnit.Framework;

namespace CodeIndex.Test
{
    [ExcludeFromCodeCoverage]
    public class ArgumentValidationTest
    {
        [Test]
        public void TestRequireContainsElement()
        {
            Assert.Throws<ArgumentException>(() => ArgumentValidation.RequireContainsElement<IEnumerable<string>>(null, "A"));
            Assert.Throws<ArgumentException>(() => ArgumentValidation.RequireContainsElement(Array.Empty<string>(), "A"));
            Assert.DoesNotThrow(() => ArgumentValidation.RequireContainsElement(new[] { 1, 2, 3 }, "A"));
        }

        [Test]
        public void TestRequireNotNullOrEmpty()
        {
            Assert.Throws<ArgumentException>(() => ArgumentValidation.RequireNotNullOrEmpty(null, "A"));
            Assert.Throws<ArgumentException>(() => ArgumentValidation.RequireNotNullOrEmpty(string.Empty, "A"));
            Assert.DoesNotThrow(() => ArgumentValidation.RequireNotNullOrEmpty("ABC", "A"));
        }

        [Test]
        public void TestRequireNotNull()
        {
            Assert.Throws<ArgumentException>(() => ArgumentValidation.RequireNotNull(null, "A"));
            Assert.DoesNotThrow(() => ArgumentValidation.RequireNotNull(string.Empty, "A"));
        }

        [Test]
        public void TestRequireRange()
        {
            Assert.Throws<ArgumentException>(() => ArgumentValidation.RequireRange(123, "A", 100, 1));
            Assert.Throws<ArgumentException>(() => ArgumentValidation.RequireRange(0, "A", 100, 1));
            Assert.DoesNotThrow(() => ArgumentValidation.RequireRange(12, "A", 100, 1));
        }
    }
}
