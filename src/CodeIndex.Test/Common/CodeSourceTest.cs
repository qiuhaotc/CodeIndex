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
            Assert.That(source.CodePK, Is.Not.EqualTo(string.Empty));
            Assert.DoesNotThrow(() => new Guid(source.CodePK));
        }

        [Test]
        public void TestGetCodeSource()
        {
            var dateTime = DateTime.UtcNow;
            var fileInfo = new FileInfo("AAA.txt");
            var source = CodeSource.GetCodeSource(fileInfo, "ABCD");
            Assert.That(source.FileName, Is.EqualTo("AAA.txt"));
            Assert.That(source.Content, Is.EqualTo("ABCD"));
            Assert.That(source.FileExtension, Is.EqualTo("txt"));
            Assert.That(source.FilePath, Is.EqualTo(fileInfo.FullName));
            Assert.That(source.LastWriteTimeUtc, Is.EqualTo(fileInfo.LastWriteTimeUtc));
            Assert.That(source.IndexDate, Is.GreaterThanOrEqualTo(dateTime));
            Assert.That(source.CodePK, Is.Not.EqualTo(string.Empty));
            Assert.DoesNotThrow(() => new Guid(source.CodePK));
            Assert.That(source.Info, Is.EqualTo($"FileName: {source.FileName}{Environment.NewLine}FilePath: {source.FilePath}{Environment.NewLine}Index Date: {source.IndexDate.ToLocalTime()}{Environment.NewLine}Last Modify Date:{source.LastWriteTimeUtc.ToLocalTime()}"));
        }
    }
}
