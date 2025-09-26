using System;
using CodeIndex.Common;
using NUnit.Framework;

namespace CodeIndex.Test
{
    public class PendingRetrySourceTest
    {
        [Test]
        public void TestConstructor()
        {
            var source = new PendingRetrySource
            {
                LastRetryUTCDate = new DateTime(2022,1,1),
                RetryTimes = 2,
                ChangedUTCDate = new DateTime(2021, 1, 1),
                ChangesType = System.IO.WatcherChangeTypes.Renamed,
                FilePath = "A",
                OldPath = "B"
            };

            Assert.That(source.ChangedUTCDate, Is.EqualTo(new DateTime(2021, 1, 1)));
            Assert.That(source.ChangesType, Is.EqualTo(System.IO.WatcherChangeTypes.Renamed));
            Assert.That(source.FilePath, Is.EqualTo("A"));
            Assert.That(source.OldPath, Is.EqualTo("B"));
            Assert.That(source.LastRetryUTCDate, Is.EqualTo(new DateTime(2022, 1, 1)));
            Assert.That(source.RetryTimes, Is.EqualTo(2));
        }
    }
}
