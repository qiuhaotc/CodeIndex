using System;
using System.IO;
using NUnit.Framework;

namespace CodeIndex.Test
{
    public class BaseTestLight
    {
        protected string TempDir { get; set; }
        protected string TempIndexDir => Path.Combine(TempDir, "IndexFolder");
        public string MonitorFolder => Path.Combine(TempDir, "CodeFolder");

        [SetUp]
        protected virtual void SetUp()
        {
            TempDir = Path.Combine(Path.GetTempPath(), "CodeIndex.Test_" + Guid.NewGuid());

            var dir = new DirectoryInfo(TempDir);
            if (!dir.Exists)
            {
                dir.Create();
            }
        }

        [TearDown]
        protected virtual void TearDown()
        {
            DeleteAllFilesInTempDir(TempDir);
        }

        void DeleteAllFilesInTempDir(string srcPath)
        {
            var dir = new DirectoryInfo(srcPath);
            dir.Delete(true);
        }
    }
}
