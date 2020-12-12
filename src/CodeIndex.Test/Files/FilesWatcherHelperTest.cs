using System;
using System.IO;
using System.Runtime.InteropServices;
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
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var renameHit = 0;
                var changeHit = 0;
                var waitMS = 100;
                Directory.CreateDirectory(Path.Combine(TempDir, "SubDir"));

                using var watcher = FilesWatcherHelper.StartWatch(TempDir, OnChangedHandler, OnRenameHandler);

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

                File.Create(Path.Combine(TempDir, "SubDir", "AAA.cs")).Close();
                Thread.Sleep(waitMS);
                Assert.IsTrue(changeHit == 4 || changeHit == 5, "Different behavior under different machines, not important due to logic doesn't care about the folder change events");
                Assert.AreEqual(1, renameHit);

                File.AppendAllText(Path.Combine(TempDir, "SubDir", "AAA.cs"), "AA BB");
                Thread.Sleep(waitMS);
                Assert.AreEqual(6, changeHit, "One for folder, one for file");
                Assert.AreEqual(1, renameHit);

                Directory.Move(Path.Combine(TempDir, "SubDir"), Path.Combine(TempDir, "SubDir2"));
                Thread.Sleep(waitMS);
                Assert.AreEqual(6, changeHit);
                Assert.AreEqual(2, renameHit);

                Directory.CreateDirectory(Path.Combine(TempDir, "SubDir3"));
                Thread.Sleep(waitMS);
                Assert.AreEqual(7, changeHit);
                Assert.AreEqual(2, renameHit);

                File.Create(Path.Combine(TempDir, "CCCC")).Close();
                Thread.Sleep(waitMS);
                Assert.AreEqual(8, changeHit);
                Assert.AreEqual(2, renameHit);

                File.SetLastAccessTime(Path.Combine(TempDir, "CCCC"), DateTime.Now.AddDays(1));
                Thread.Sleep(waitMS);
                Assert.AreEqual(8, changeHit, "Do not watch last access time for file");
                Assert.AreEqual(2, renameHit);

                void OnRenameHandler(object sender, RenamedEventArgs e)
                {
                    renameHit++;
                }

                void OnChangedHandler(object sender, FileSystemEventArgs e)
                {
                    changeHit++;
                }
            }
            else
            {
                Assert.Pass();
            }
        }
    }
}
