using System;
using System.IO;
using CodeIndex.Common;
using CodeIndex.IndexBuilder;
using CodeIndex.Search;
using NUnit.Framework;

namespace CodeIndex.Test
{
    public class BaseTest
    {
        protected string TempDir { get; set; }
        string TempIndexDir => Path.Combine(TempDir, "IndexFolder");
        public string MonitorFolder => Path.Combine(TempDir, "CodeFolder");
        public CodeIndexConfiguration Config => new CodeIndexConfiguration
        {
            LuceneIndex = TempIndexDir,
            MonitorFolder = MonitorFolder
        };

        QueryGenerator generator;
        protected QueryGenerator Generator => generator ??= new QueryGenerator();

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
            LucenePool.SaveResultsAndClearLucenePool(Config);
            DeleteAllFilesInTempDir(TempDir);
        }

        void DeleteAllFilesInTempDir(string srcPath)
        {
            var dir = new DirectoryInfo(srcPath);
            dir.Delete(true);
        }
    }
}
