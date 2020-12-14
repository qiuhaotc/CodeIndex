using System;
using System.IO;
using CodeIndex.Common;
using NUnit.Framework;

namespace CodeIndex.Test
{
    public class BaseTestLight
    {
        protected string TempDir { get; set; }
        protected string TempIndexDir => Path.Combine(TempDir, "IndexFolder");
        public string MonitorFolder => Path.Combine(TempDir, "CodeFolder");
        protected string TempCodeIndexDir => Path.Combine(TempIndexDir, CodeIndexConfiguration.CodeIndexesFolder, CodeIndexConfiguration.CodeIndexFolder);
        protected string TempHintIndexDir => Path.Combine(TempIndexDir, CodeIndexConfiguration.CodeIndexesFolder, CodeIndexConfiguration.HintIndexFolder);
        protected ILog Log => log ??= new DummyLog();
        ILog log;

        [SetUp]
        protected virtual void SetUp()
        {
            TempDir = Path.Combine(Path.GetTempPath(), "CodeIndex.Test_" + Guid.NewGuid());

            Directory.CreateDirectory(TempDir);
            Directory.CreateDirectory(MonitorFolder);
        }

        [TearDown]
        protected virtual void TearDown()
        {
            DeleteAllFilesInTempDir(TempDir);
        }

        void DeleteAllFilesInTempDir(string srcPath)
        {
            Directory.Delete(srcPath, true);
        }
    }
}
