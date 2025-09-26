using System;
using System.IO;
using CodeIndex.Common;
using CodeIndex.IndexBuilder;
using Lucene.Net.Documents;
using NUnit.Framework;

namespace CodeIndex.Test
{
    public class IndexBuilderHelperTest
    {
        [Test]
        public void TestToStringSafe()
        {
            string a = null;
            Assert.That(a.ToStringSafe(), Is.EqualTo(string.Empty));
            Assert.That(string.Empty.ToStringSafe(), Is.EqualTo(string.Empty));
            Assert.That("ABC".ToStringSafe(), Is.EqualTo("ABC"));
        }

        [Test]
        public void TestToLowerSafe()
        {
            string a = null;
            Assert.That(a.ToLowerSafe(), Is.EqualTo(string.Empty));
            Assert.That(string.Empty.ToLowerSafe(), Is.EqualTo(string.Empty));
            Assert.That("ABC".ToLowerSafe(), Is.EqualTo("abc"));
        }

        [Test]
        public void TestGetDocumentFromSource()
        {
            var codeSource = new CodeSource
            {
                FileName = "Dummy File 2",
                FileExtension = "cs",
                FilePath = @"C:\Dummy File 2.cs",
                Content = "Test Content" + Environment.NewLine + "A New Line For Test"
            };

            var document = IndexBuilderHelper.GetDocumentFromSource(codeSource);
            AssertFields(document);

            var convertBack = document.GetObject<CodeSource>();
            Assert.Multiple(() =>
            {
                Assert.That(convertBack.FilePath, Is.EqualTo(codeSource.FilePath));
                Assert.That(convertBack.Content, Is.EqualTo(codeSource.Content));
                Assert.That(convertBack.IndexDate, Is.EqualTo(codeSource.IndexDate));
                Assert.That(convertBack.LastWriteTimeUtc, Is.EqualTo(codeSource.LastWriteTimeUtc));
                Assert.That(convertBack.CodePK, Is.EqualTo(codeSource.CodePK));
                Assert.That(convertBack.FileName, Is.EqualTo(codeSource.FileName));
                Assert.That(convertBack.FileExtension, Is.EqualTo(codeSource.FileExtension));
                Assert.That(convertBack.Info, Is.EqualTo(codeSource.Info));
            });

            codeSource = new CodeSource
            {
                FileName = "Dummy File 2",
                FileExtension = "CS",
                FilePath = @"C:\Dummy File 2.CS",
                Content = "Test Content" + Environment.NewLine + "A New Line For Test"
            };

            document = IndexBuilderHelper.GetDocumentFromSource(codeSource);
            Assert.That(document.Get(nameof(CodeSource.FileExtension)), Is.EqualTo("cs"), "Lowercase the extension");
        }

        [Test]
        public void TestRenameIndex_PathChanged()
        {
            var codeSource = new CodeSource
            {
                FileName = "Dummy File 2",
                FileExtension = "cs",
                FilePath = @"C:\AAAA\Dummy File 2.cs",
                Content = "Test Content" + Environment.NewLine + "A New Line For Test"
            };

            var document = IndexBuilderHelper.GetDocumentFromSource(codeSource);
            AssertFields(document);
            Assert.That(document.Get(nameof(CodeSource.FilePath)), Is.EqualTo(@"C:\AAAA\Dummy File 2.cs"));

            document = IndexBuilderHelper.RenameIndexForFolder(document, @"C:\AAAA", @"C:\BBBB");
            AssertFields(document);
            Assert.That(document.Get(nameof(CodeSource.FilePath)), Is.EqualTo(@"C:\BBBB\Dummy File 2.cs"));
        }

        [Test]
        public void TestRenameIndex_FileChanged()
        {
            var filePath = Path.Combine(Path.GetTempPath(), "Dummy File 2.cs");

            var codeSource = new CodeSource
            {
                FileName = "Dummy File 2.cs",
                FileExtension = "cs",
                FilePath = filePath,
                Content = "Test Content" + Environment.NewLine + "A New Line For Test"
            };

            var document = IndexBuilderHelper.GetDocumentFromSource(codeSource);
            Assert.That(document.Get(nameof(CodeSource.FileExtension)), Is.EqualTo("cs"));

            var newPath = Path.Combine(Path.GetTempPath(), "Dummy File 2.CS");
            document = IndexBuilderHelper.RenameIndexForFile(document, newPath);
            AssertFields(document);
            Assert.That(document.Get(nameof(CodeSource.FileExtension)), Is.EqualTo("cs"), "Extension is case-insensitive, still 'cs'");
            Assert.That(document.Get(nameof(CodeSource.FileName)), Is.EqualTo("Dummy File 2.CS"), "File name changed");
            Assert.That(document.Get(nameof(CodeSource.FilePath)), Is.EqualTo(newPath));
            Assert.That(document.Get(nameof(CodeSource.FilePath) + Constants.NoneTokenizeFieldSuffix), Is.EqualTo(newPath));

            newPath = Path.Combine(Path.GetTempPath(), "Dummy File 3.TXT");
            document = IndexBuilderHelper.RenameIndexForFile(document, newPath);
            AssertFields(document);
            Assert.That(document.Get(nameof(CodeSource.FileExtension)), Is.EqualTo("txt"), "Extension is changed to 'txt' and lowercase");
            Assert.That(document.Get(nameof(CodeSource.FileName)), Is.EqualTo("Dummy File 3.TXT"), "File name changed");
            Assert.That(document.Get(nameof(CodeSource.FilePath)), Is.EqualTo(newPath));
            Assert.That(document.Get(nameof(CodeSource.FilePath) + Constants.NoneTokenizeFieldSuffix), Is.EqualTo(newPath));
        }

        void AssertFields(Document document)
        {
            Assert.That(document.Fields.Count, Is.EqualTo(9));
            Assert.Multiple(() =>
            {
                Assert.That(document.GetField(nameof(CodeSource.CodePK)) is StringField, Is.True);
                Assert.That(document.GetField(nameof(CodeSource.Content)) is TextField, Is.True);
                Assert.That(document.GetField(nameof(CodeSource.Content) + Constants.CaseSensitive) is TextField, Is.True);
                Assert.That(document.GetField(nameof(CodeSource.IndexDate)) is Int64Field, Is.True);
                Assert.That(document.GetField(nameof(CodeSource.LastWriteTimeUtc)) is Int64Field, Is.True);
                Assert.That(document.GetField(nameof(CodeSource.FileName)) is TextField, Is.True);
                Assert.That(document.GetField(nameof(CodeSource.FileExtension)) is StringField, Is.True);
                Assert.That(document.GetField(nameof(CodeSource.FilePath)) is TextField, Is.True);
                Assert.That(document.GetField(nameof(CodeSource.FilePath) + Constants.NoneTokenizeFieldSuffix) is StringField, Is.True);
            });
        }
    }
}
