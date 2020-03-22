using System;
using System.IO;
using CodeIndex.Common;
using NUnit.Framework;

namespace CodeIndex.Test
{
    public class CodeSourceTest
    {
        [Test]
        public void TestConstructor()
        {
            var source = new CodeSource();
            Assert.AreNotEqual(string.Empty, source.CodePK);
            Assert.DoesNotThrow(() => new Guid(source.CodePK));
        }

        [Test]
        public void TestGetCodeSource()
        {
            var dateTime = DateTime.UtcNow;
            var fileInfo = new FileInfo("C:\\AAA.txt");
            var source = CodeSource.GetCodeSource(fileInfo, "ABCD");
            Assert.AreEqual("AAA.txt", source.FileName);
            Assert.AreEqual("ABCD", source.Content);
            Assert.AreEqual("txt", source.FileExtension);
            Assert.AreEqual("C:\\AAA.txt", source.FilePath);
            Assert.AreEqual(fileInfo.LastWriteTimeUtc, source.LastWriteTimeUtc);
            Assert.LessOrEqual(dateTime, source.IndexDate);
            Assert.AreNotEqual(string.Empty, source.CodePK);
            Assert.DoesNotThrow(() => new Guid(source.CodePK));
            Assert.AreEqual($"FileName: {source.FileName}{Environment.NewLine}FilePath: {source.FilePath}{Environment.NewLine}Index Date: {source.IndexDate.ToLocalTime()}{Environment.NewLine}Last Modify Date:{source.LastWriteTimeUtc.ToLocalTime()}", source.Info);
        }
    }
}
