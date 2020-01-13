using System.IO;
using System.Threading;
using CodeIndex.Files;
using NUnit.Framework;

namespace CodeIndex.Test
{
    public class FilesWatcherHelperTest : BaseTest
    {
        [Test]
        public void TestStartWatch()
        {
            var renameHit = 0;
            var changeHit = 0;
            var waitMS = 10;

            using (var watcher = FilesWatcherHelper.StartWatch(TempDir, OnchangedHandler, OnRenameHandler))
            {
                File.Create(Path.Combine(TempDir, "AAA.cs")).Close();
                Thread.Sleep(waitMS);
                Assert.AreEqual(1, changeHit);
                Assert.AreEqual(0, renameHit);

                File.AppendAllText(Path.Combine(TempDir, "AAA.cs"), "12345");
                Thread.Sleep(waitMS);
                Assert.AreEqual(2, changeHit);
                Assert.AreEqual(0, renameHit);

                File.Move(Path.Combine(TempDir, "AAA.cs"), Path.Combine(TempDir, "BBB.cs"));
                Thread.Sleep(waitMS);
                Assert.AreEqual(2, changeHit);
                Assert.AreEqual(1, renameHit);

                File.Delete(Path.Combine(TempDir, "BBB.cs"));
                Thread.Sleep(waitMS);
                Assert.AreEqual(3, changeHit);
                Assert.AreEqual(1, renameHit);
            }

            void OnRenameHandler(object sender, RenamedEventArgs e)
            {
                renameHit++;
            }

            void OnchangedHandler(object sender, FileSystemEventArgs e)
            {
                changeHit++;
            }
        }
    }
}
