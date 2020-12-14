using System;
using CodeIndex.Common;
using CodeIndex.IndexBuilder;
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
            Assert.AreEqual(8, document.Fields.Count);
            Assert.NotNull(document.Get(nameof(CodeSource.FilePath) + Constants.NoneTokenizeFieldSuffix));

            var convertBack = document.GetObject<CodeSource>();
            Assert.AreEqual(codeSource.FilePath, convertBack.FilePath);
            Assert.AreEqual(codeSource.Content, convertBack.Content);
            Assert.AreEqual(codeSource.IndexDate, convertBack.IndexDate);
            Assert.AreEqual(codeSource.LastWriteTimeUtc, convertBack.LastWriteTimeUtc);
            Assert.AreEqual(codeSource.CodePK, convertBack.CodePK);
            Assert.AreEqual(codeSource.FileName, convertBack.FileName);
            Assert.AreEqual(codeSource.FileExtension, convertBack.FileExtension);
            Assert.AreEqual(codeSource.Info, convertBack.Info);
        }
    }
}
