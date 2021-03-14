using System;
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
            Assert.AreEqual(string.Empty, a.ToStringSafe());
            Assert.AreEqual(string.Empty, string.Empty.ToStringSafe());
            Assert.AreEqual("ABC", "ABC".ToStringSafe());
        }

        [Test]
        public void TestToLowerSafe()
        {
            string a = null;
            Assert.AreEqual(string.Empty, a.ToLowerSafe());
            Assert.AreEqual(string.Empty, string.Empty.ToLowerSafe());
            Assert.AreEqual("abc", "ABC".ToLowerSafe());
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
            Assert.AreEqual(codeSource.FilePath, convertBack.FilePath);
            Assert.AreEqual(codeSource.Content, convertBack.Content);
            Assert.AreEqual(codeSource.IndexDate, convertBack.IndexDate);
            Assert.AreEqual(codeSource.LastWriteTimeUtc, convertBack.LastWriteTimeUtc);
            Assert.AreEqual(codeSource.CodePK, convertBack.CodePK);
            Assert.AreEqual(codeSource.FileName, convertBack.FileName);
            Assert.AreEqual(codeSource.FileExtension, convertBack.FileExtension);
            Assert.AreEqual(codeSource.Info, convertBack.Info);

            codeSource = new CodeSource
            {
                FileName = "Dummy File 2",
                FileExtension = "CS",
                FilePath = @"C:\Dummy File 2.CS",
                Content = "Test Content" + Environment.NewLine + "A New Line For Test"
            };

            document = IndexBuilderHelper.GetDocumentFromSource(codeSource);
            Assert.AreEqual("cs", document.Get(nameof(CodeSource.FileExtension)), "Lowercase the extension");
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
            Assert.AreEqual(@"C:\AAAA\Dummy File 2.cs", document.Get(nameof(CodeSource.FilePath)));

            document = IndexBuilderHelper.RenameIndexForFolder(document, @"C:\AAAA", @"C:\BBBB");
            AssertFields(document);
            Assert.AreEqual(@"C:\BBBB\Dummy File 2.cs", document.Get(nameof(CodeSource.FilePath)));
        }

        [Test]
        public void TestRenameIndex_FileChanged()
        {
            var codeSource = new CodeSource
            {
                FileName = "Dummy File 2.cs",
                FileExtension = "cs",
                FilePath = @"C:\Dummy File 2.cs",
                Content = "Test Content" + Environment.NewLine + "A New Line For Test"
            };

            var document = IndexBuilderHelper.GetDocumentFromSource(codeSource);
            Assert.AreEqual("cs", document.Get(nameof(CodeSource.FileExtension)));

            document = IndexBuilderHelper.RenameIndexForFile(document, @"C:\Dummy File 2.CS");
            AssertFields(document);
            Assert.AreEqual("cs", document.Get(nameof(CodeSource.FileExtension)), "Extension is case-insensitive, still 'cs'");
            Assert.AreEqual("Dummy File 2.CS", document.Get(nameof(CodeSource.FileName)), "File name changed");
            Assert.AreEqual(@"C:\Dummy File 2.CS", document.Get(nameof(CodeSource.FilePath)));
            Assert.AreEqual(@"C:\Dummy File 2.CS", document.Get(nameof(CodeSource.FilePath) + Constants.NoneTokenizeFieldSuffix));

            document = IndexBuilderHelper.RenameIndexForFile(document, @"C:\Dummy File 3.TXT");
            AssertFields(document);
            Assert.AreEqual("txt", document.Get(nameof(CodeSource.FileExtension)), "Extension is changed to 'txt' and lowercase");
            Assert.AreEqual("Dummy File 3.TXT", document.Get(nameof(CodeSource.FileName)), "File name changed");
            Assert.AreEqual(@"C:\Dummy File 3.TXT", document.Get(nameof(CodeSource.FilePath)));
            Assert.AreEqual(@"C:\Dummy File 3.TXT", document.Get(nameof(CodeSource.FilePath) + Constants.NoneTokenizeFieldSuffix));
        }

        void AssertFields(Document document)
        {
            Assert.AreEqual(9, document.Fields.Count);
            Assert.Multiple(() =>
            {
                Assert.IsTrue(document.GetField(nameof(CodeSource.CodePK)) is StringField);
                Assert.IsTrue(document.GetField(nameof(CodeSource.Content)) is TextField);
                Assert.IsTrue(document.GetField(nameof(CodeSource.Content) + Constants.CaseSensitive) is TextField);
                Assert.IsTrue(document.GetField(nameof(CodeSource.IndexDate)) is Int64Field);
                Assert.IsTrue(document.GetField(nameof(CodeSource.LastWriteTimeUtc)) is Int64Field);
                Assert.IsTrue(document.GetField(nameof(CodeSource.FileName)) is TextField);
                Assert.IsTrue(document.GetField(nameof(CodeSource.FileExtension)) is StringField);
                Assert.IsTrue(document.GetField(nameof(CodeSource.FilePath)) is TextField);
                Assert.IsTrue(document.GetField(nameof(CodeSource.FilePath) + Constants.NoneTokenizeFieldSuffix) is StringField);
            });
        }
    }
}
