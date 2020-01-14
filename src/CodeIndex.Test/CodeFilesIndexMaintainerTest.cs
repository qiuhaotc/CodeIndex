using System.IO;
using System.Threading;
using CodeIndex.Common;
using CodeIndex.IndexBuilder;
using CodeIndex.MaintainIndex;
using Lucene.Net.Index;
using Lucene.Net.Search;
using NUnit.Framework;

namespace CodeIndex.Test
{
    public class CodeFilesIndexMaintainerTest : BaseTest
    {
        [Test]
        public void TestMaintainerIndex()
        {
            var waitMS = 10000;
            var watchPath = Path.Combine(TempDir, "SubDir");
            Directory.CreateDirectory(watchPath);
            Directory.CreateDirectory(Path.Combine(watchPath, "FolderA"));
            Directory.CreateDirectory(Path.Combine(watchPath, "FolderB"));

            var fileAPath = Path.Combine(watchPath, "FolderA", "AAA.cs");
            File.Create(fileAPath).Close();
            File.AppendAllText(fileAPath, "12345");

            var fileBPath = Path.Combine(watchPath, "FolderB", "BBB.xml");
            File.Create(fileBPath).Close();
            File.AppendAllText(fileBPath, "this is a content for test, that's it\r\na new line;");

            CodeIndexBuilder.BuildIndex(TempIndexDir, true, true,
                CodeSource.GetCodeSource(new DirectoryInfo(fileAPath)),
                CodeSource.GetCodeSource(new DirectoryInfo(fileBPath)));
            CodeIndexBuilder.CloseIndexWriterAndCommitChange(TempIndexDir);

            using (var maintainer = new CodeFilesIndexMaintainer(watchPath, TempIndexDir, new[] {"dll"}, new[] {""}))
            {
                File.AppendAllText(fileAPath, "56789");
                Thread.Sleep(waitMS);

                var index = CodeIndexSearcher.Search(TempIndexDir, new TermQuery(new Term(nameof(CodeSource.FileName), "AAA.cs")), 10);
                Assert.AreEqual(1, index.Length);
                Assert.AreEqual("1234556789", index[0].Get(nameof(CodeSource.Content)));
            }
        }
    }
}
