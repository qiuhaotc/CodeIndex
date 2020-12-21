using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CodeIndex.Common;
using CodeIndex.Files;
using NUnit.Framework;

namespace CodeIndex.Test
{
    public class FileChangesPreProcessingTest
    {
        [Test]
        public void TestRemoveTemplateChanges()
        {
            var listChanges = new List<ChangedSource>
            {
                new ()
                {
                    ChangedUTCDate = new DateTime(2020, 1, 1),
                    ChangesType = WatcherChangeTypes.Renamed,
                    FilePath = "D:\\A.txt",
                    OldPath = "D:\\b.txt"
                },
                new ()
                {
                    ChangedUTCDate = new DateTime(2020, 1, 2),
                    ChangesType = WatcherChangeTypes.Renamed,
                    FilePath = "D:\\B.txt",
                    OldPath = "D:\\a.txt"
                },
                new ()
                {
                    ChangedUTCDate = new DateTime(2020, 1, 3),
                    ChangesType = WatcherChangeTypes.Renamed,
                    FilePath = "D:\\d.txt",
                    OldPath = "D:\\e.txt"
                }
            };

            listChanges.PreProcessingChanges(string.Empty, indexConfig, log);
            CollectionAssert.AreEquivalent(new[]
                {
                    (new DateTime(2020, 1, 1), WatcherChangeTypes.Changed, "D:\\b.txt", (string)null),
                    (new DateTime(2020, 1, 3), WatcherChangeTypes.Renamed, "D:\\d.txt", "D:\\e.txt")
                },
                listChanges.Select(u => (u.ChangedUTCDate, u.ChangesType, u.FilePath, u.OldPath)));
        }


        [Test]
        public void TestRemoveTemplateDeletedChanges()
        {
            var listChanges = new List<ChangedSource>
            {
                new ()
                {
                    ChangedUTCDate = new DateTime(2020, 1, 1),
                    ChangesType = WatcherChangeTypes.Renamed,
                    FilePath = "D:\\a.txt",
                    OldPath = "D:\\B.txt"
                },
                new()
                {
                    ChangedUTCDate = new DateTime(2020, 1, 2),
                    ChangesType = WatcherChangeTypes.Renamed,
                    FilePath = "D:\\b.txt",
                    OldPath = "D:\\A.txt"
                },
                new ()
                {
                    ChangedUTCDate = new DateTime(2020, 1, 5),
                    ChangesType = WatcherChangeTypes.Deleted,
                    FilePath = "D:\\c.txt",
                },
                new ()
                {
                    ChangedUTCDate = new DateTime(2020, 1, 7),
                    ChangesType = WatcherChangeTypes.Created,
                    FilePath = "D:\\C.txt",
                }
            };

            listChanges.PreProcessingChanges(string.Empty, indexConfig, log);
            CollectionAssert.AreEquivalent(new[]
                {
                    (new DateTime(2020, 1, 1), WatcherChangeTypes.Changed, "D:\\B.txt", (string)null),
                    (new DateTime(2020, 1, 7), WatcherChangeTypes.Changed, "D:\\C.txt", null),
                },
                listChanges.Select(u => (u.ChangedUTCDate, u.ChangesType, u.FilePath, u.OldPath)));
        }


        [Test]
        public void TestRemoveDuplicatedChanges()
        {
            var listChanges = new List<ChangedSource>
            {
                new ()
                {
                    ChangedUTCDate = new DateTime(2020, 1, 1),
                    ChangesType = WatcherChangeTypes.Renamed,
                    FilePath = "D:\\a.txt",
                    OldPath = "D:\\B.txt"
                },
                new()
                {
                    ChangedUTCDate = new DateTime(2020, 1, 2),
                    ChangesType = WatcherChangeTypes.Renamed,
                    FilePath = "D:\\b.txt",
                    OldPath = "D:\\A.txt"
                },
                new ()
                {
                    ChangedUTCDate = new DateTime(2020, 1, 5),
                    ChangesType = WatcherChangeTypes.Deleted,
                    FilePath = "D:\\c.txt",
                },
                new ()
                {
                    ChangedUTCDate = new DateTime(2020, 1, 7),
                    ChangesType = WatcherChangeTypes.Created,
                    FilePath = "D:\\C.txt",
                },
                new ()
                {
                    ChangedUTCDate = new DateTime(2020, 1, 7),
                    ChangesType = WatcherChangeTypes.Changed,
                    FilePath = "D:\\E.txt",
                },
                new ()
                {
                    ChangedUTCDate = new DateTime(2020, 1, 8),
                    ChangesType = WatcherChangeTypes.Changed,
                    FilePath = "D:\\E.txt",
                },
                new ()
                {
                    ChangedUTCDate = new DateTime(2020, 1, 9),
                    ChangesType = WatcherChangeTypes.Changed,
                    FilePath = "D:\\e.txt",
                }
            };

            listChanges.PreProcessingChanges(string.Empty, indexConfig, log);
            CollectionAssert.AreEquivalent(new[]
                {
                    (new DateTime(2020, 1, 1), WatcherChangeTypes.Changed, "D:\\B.txt", (string)null),
                    (new DateTime(2020, 1, 7), WatcherChangeTypes.Changed, "D:\\C.txt", null),
                    (new DateTime(2020, 1, 9), WatcherChangeTypes.Changed, "D:\\e.txt", null),
                },
                listChanges.Select(u => (u.ChangedUTCDate, u.ChangesType, u.FilePath, u.OldPath)));
        }

        readonly DummyLog log = new();

        readonly IndexConfig indexConfig = new();
    }
}
