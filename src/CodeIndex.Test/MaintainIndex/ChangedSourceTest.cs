using System;
using CodeIndex.MaintainIndex;
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

            Assert.AreEqual(new DateTime(2021,1,1), source.ChangedUTCDate);
            Assert.AreEqual(System.IO.WatcherChangeTypes.Renamed, source.ChangesType);
            Assert.AreEqual("A", source.FilePath);
            Assert.AreEqual("B", source.OldPath);
        }
    }
}
