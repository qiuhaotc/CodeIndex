using System.Collections.Generic;
using System.IO;
using System.Text;
using CodeIndex.Files;
using NUnit.Framework;

namespace CodeIndex.Test
{
    public class FilesContentHelperTest : BaseTest
    {
        [Test]
        public void TestGetContent_ReadContentUsedByAnotherProcess()
        {
            using var stream1 = File.Create(Path.Combine(TempDir, "AAA.cs"));
            stream1.Write(Encoding.UTF8.GetBytes("这是一个例句"));
            stream1.Close();

            using var stream2 = new FileStream(Path.Combine(TempDir, "AAA.cs"), FileMode.Open, FileAccess.Write);
            var content = FilesContentHelper.ReadAllText(Path.Combine(TempDir, "AAA.cs"));
            Assert.That(content, Is.EqualTo("这是一个例句"), "Can read file content used by another process");
        }

        [Test]
        [TestCaseSource(nameof(EncodingTestCases))]
        public void TestGetContent_EncodingCorrect((Encoding Encoding, string Name) encodingWithName)
        {
            var filePath = Path.Combine(TempDir, "AAA.cs");
            File.WriteAllText(filePath, "这是一个例句", encodingWithName.Encoding);

            var content = FilesContentHelper.ReadAllText(Path.Combine(TempDir, "AAA.cs"));
            Assert.That(content, Is.EqualTo("这是一个例句"), $"Test Under {encodingWithName.Name}");
        }

        static IEnumerable<(Encoding Encoding, string Name)> EncodingTestCases()
        {
            yield return (Encoding.UTF8, "UTF-8");
            yield return (Encoding.Unicode, "UTF-16LE");
            yield return (Encoding.BigEndianUnicode, "UTF-16BE");
            yield return (Encoding.UTF32, "UTF-32");
        }
    }
}
