using CodeIndex.Common;
using NUnit.Framework;
using System;

namespace CodeIndex.Test
{
    class IndexStatusInfoTest
    {
        [Test]
        public void TestConstructor()
        {
            var statusInfo = new IndexStatusInfo(IndexStatus.Initializing, new IndexConfig());
            Assert.That(statusInfo.IndexConfig, Is.Not.Null);
            Assert.That(statusInfo.IndexStatus, Is.EqualTo(IndexStatus.Initializing));
            Assert.Throws<ArgumentException>(() => _ = new IndexStatusInfo(default, null));
        }
    }
}
