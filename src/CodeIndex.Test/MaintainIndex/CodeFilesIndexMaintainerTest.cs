using System;
using System.IO;
using System.Threading;
using CodeIndex.Common;
using CodeIndex.IndexBuilder;
using CodeIndex.MaintainIndex;
using CodeIndex.Search;
using NUnit.Framework;

namespace CodeIndex.Test
{
    public class CodeFilesIndexMaintainerTest : BaseTest
    {
        [Test]
        public void TestMaintainerIndex()
        {
            var waitMS = 2000;
            Directory.CreateDirectory(MonitorFolder);
            Directory.CreateDirectory(Path.Combine(MonitorFolder, "FolderA"));
            Directory.CreateDirectory(Path.Combine(MonitorFolder, "FolderB"));

            var fileAPath = Path.Combine(MonitorFolder, "FolderA", "AAA.cs");
            File.Create(fileAPath).Close();
            File.AppendAllText(fileAPath, "12345");

            var fileBPath = Path.Combine(MonitorFolder, "FolderB", "BBB.xml");
            File.Create(fileBPath).Close();
            File.AppendAllText(fileBPath, "this is a content for test, that's it\r\na new line;");

            CodeIndexBuilder.BuildIndex(Config, true, true, true,
                new[] { CodeSource.GetCodeSource(new FileInfo(fileAPath), "12345"),
                CodeSource.GetCodeSource(new FileInfo(fileBPath), "this is a content for test, that's it\r\na new line;") });
            LucenePool.SaveResultsAndClearLucenePool(Config.LuceneIndexForCode);

            using var maintainer = new CodeFilesIndexMaintainer(Config, new[] { "dll" }, Array.Empty<string>(), 1);
            maintainer.StartWatch();
            maintainer.SetInitalizeFinishedToTrue();
            File.AppendAllText(fileAPath, "56789");
            Thread.Sleep(waitMS); // wait task finish saving

            var index = CodeIndexSearcher.Search(Config.LuceneIndexForCode, Generator.GetQueryFromStr($"{nameof(CodeSource.FileName)}:\"AAA.cs\""), 10);
            Assert.AreEqual(1, index.Length);
            Assert.AreEqual("1234556789", index[0].Get(nameof(CodeSource.Content)));
        }

        // TODO: Test False Tolerance
        // TODO: Test Directoy Change
    }
}
