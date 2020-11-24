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
            Assert.Throws<ArgumentException>(() => Array.Empty<string>().RequireContainsElement("A"));
            Assert.DoesNotThrow(() => new[] { 1, 2, 3 }.RequireContainsElement("A"));
        }

        [Test]
        public void TestRequireNotNullOrEmpty()
        {
            Assert.Throws<ArgumentException>(() => ArgumentValidation.RequireNotNullOrEmpty(null, "A"));
            Assert.Throws<ArgumentException>(() => string.Empty.RequireNotNullOrEmpty("A"));
            Assert.DoesNotThrow(() => "ABC".RequireNotNullOrEmpty("A"));
        }

        [Test]
        public void TestRequireNotNull()
        {
            Assert.Throws<ArgumentException>(() => ArgumentValidation.RequireNotNull(null, "A"));
            Assert.DoesNotThrow(() => string.Empty.RequireNotNull("A"));
        }

        [Test]
        public void TestRequireRange()
        {
            Assert.Throws<ArgumentException>(() => 123.RequireRange("A", 100, 1));
            Assert.Throws<ArgumentException>(() => 0.RequireRange("A", 100, 1));
            Assert.DoesNotThrow(() => 12.RequireRange("A", 100, 1));
        }
    }
}
