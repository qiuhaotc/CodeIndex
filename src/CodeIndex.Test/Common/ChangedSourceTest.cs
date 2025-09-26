using System;
using CodeIndex.Common;
using NUnit.Framework;

namespace CodeIndex.Test
{
    public class ChangedSourceTest
    {
        [Test]
        public void TestConstructor()
        {
            var source = new ChangedSource
            {
                ChangedUTCDate = new DateTime(2021,1,1),
                ChangesType = System.IO.WatcherChangeTypes.Renamed,
                FilePath = "A",
                OldPath = "B"
            };

            // NUnit4 constraint syntax
            Assert.That(source.ChangedUTCDate, Is.EqualTo(new DateTime(2021, 1, 1)));
            Assert.That(source.ChangesType, Is.EqualTo(System.IO.WatcherChangeTypes.Renamed));
            Assert.That(source.FilePath, Is.EqualTo("A"));
            Assert.That(source.OldPath, Is.EqualTo("B"));
        }
    }
}
