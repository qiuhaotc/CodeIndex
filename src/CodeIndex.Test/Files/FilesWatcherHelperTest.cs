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
                Assert.That(changeHit, Is.EqualTo(1));
                Assert.That(renameHit, Is.EqualTo(0));

                File.AppendAllText(Path.Combine(TempDir, "AAA.cs"), "12345");
                Thread.Sleep(waitMS);
                Assert.That(changeHit, Is.EqualTo(2));
                Assert.That(renameHit, Is.EqualTo(0));

                File.Move(Path.Combine(TempDir, "AAA.cs"), Path.Combine(TempDir, "BBB.cs"));
                Thread.Sleep(waitMS);
                Assert.That(changeHit, Is.EqualTo(2));
                Assert.That(renameHit, Is.EqualTo(1));

                File.Delete(Path.Combine(TempDir, "BBB.cs"));
                Thread.Sleep(waitMS);
                Assert.That(changeHit, Is.EqualTo(3));
                Assert.That(renameHit, Is.EqualTo(1));

                File.Create(Path.Combine(TempDir, "SubDir", "AAA.cs")).Close();
                Thread.Sleep(waitMS);
                Assert.That(changeHit, Is.EqualTo(4).Or.EqualTo(5), "Different behavior under different machines, not important due to logic doesn't care about the folder change events");
                Assert.That(renameHit, Is.EqualTo(1));

                File.AppendAllText(Path.Combine(TempDir, "SubDir", "AAA.cs"), "AA BB");
                Thread.Sleep(waitMS);
                Assert.That(changeHit, Is.EqualTo(6), "One for folder, one for file");
                Assert.That(renameHit, Is.EqualTo(1));

                Directory.Move(Path.Combine(TempDir, "SubDir"), Path.Combine(TempDir, "SubDir2"));
                Thread.Sleep(waitMS);
                Assert.That(changeHit, Is.EqualTo(6));
                Assert.That(renameHit, Is.EqualTo(2));

                Directory.CreateDirectory(Path.Combine(TempDir, "SubDir3"));
                Thread.Sleep(waitMS);
                Assert.That(changeHit, Is.EqualTo(7));
                Assert.That(renameHit, Is.EqualTo(2));

                File.Create(Path.Combine(TempDir, "CCCC")).Close();
                Thread.Sleep(waitMS);
                Assert.That(changeHit, Is.EqualTo(8));
                Assert.That(renameHit, Is.EqualTo(2));

                File.SetLastAccessTime(Path.Combine(TempDir, "CCCC"), DateTime.Now.AddDays(1));
                Thread.Sleep(waitMS);
                Assert.That(changeHit, Is.EqualTo(8), "Do not watch last access time for file");
                Assert.That(renameHit, Is.EqualTo(2));

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
