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
            Assert.IsNotNull(statusInfo.IndexConfig);
            Assert.AreEqual(IndexStatus.Initializing, statusInfo.IndexStatus);
            Assert.Throws<ArgumentException>(() => _ = new IndexStatusInfo(default, null));
        }
    }
}
