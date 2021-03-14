using System.IO;
using CodeIndex.Common;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Store;

namespace CodeIndex.IndexBuilder
{
    public static class IndexBuilderHelper
    {
        public static string ToStringSafe(this string value)
        {
            return value ?? string.Empty;
        }

        public static string ToLowerSafe(this string value)
        {
            return value?.ToLowerInvariant() ?? string.Empty;
        }

        public static Document GetDocumentFromSource(CodeSource source)
        {
            return new Document
            {
                new TextField(nameof(source.FileName), source.FileName.ToStringSafe(), Field.Store.YES),
                // StringField indexes but doesn't tokenize
                new StringField(nameof(source.FileExtension), source.FileExtension.ToLowerSafe(), Field.Store.YES),
                new StringField(nameof(source.FilePath) + Constants.NoneTokenizeFieldSuffix, source.FilePath.ToStringSafe(), Field.Store.YES),
                new TextField(nameof(source.FilePath), source.FilePath.ToStringSafe(), Field.Store.YES),
                new TextField(nameof(source.Content), source.Content.ToStringSafe(), Field.Store.YES),
                new TextField(CodeIndexBuilder.GetCaseSensitiveField(nameof(source.Content)), source.Content.ToStringSafe(), Field.Store.YES),
                new Int64Field(nameof(source.IndexDate), source.IndexDate.Ticks, Field.Store.YES),
                new Int64Field(nameof(source.LastWriteTimeUtc), source.LastWriteTimeUtc.Ticks, Field.Store.YES),
                new StringField(nameof(source.CodePK), source.CodePK, Field.Store.YES)
            };
        }

        public static bool IndexExists(string luceneIndex)
        {
            luceneIndex.RequireNotNullOrEmpty(nameof(luceneIndex));

            using var dir = FSDirectory.Open(luceneIndex);
            var indexExist = DirectoryReader.IndexExists(dir);

            return indexExist;
        }

        public static Document RenameIndexForFile(this Document document, string nowFilePath)
        {
            document.RemoveField(nameof(CodeSource.FilePath));
            document.RemoveField(nameof(CodeSource.FilePath) + Constants.NoneTokenizeFieldSuffix);
            document.Add(new TextField(nameof(CodeSource.FilePath), nowFilePath.ToStringSafe(), Field.Store.YES));
            document.Add(new StringField(nameof(CodeSource.FilePath) + Constants.NoneTokenizeFieldSuffix, nowFilePath.ToStringSafe(), Field.Store.YES));

            var oldExtension = document.Get(nameof(CodeSource.FileExtension));
            var fileInfo = new FileInfo(nowFilePath);
            var nowExtension = fileInfo.Extension?.Replace(".", string.Empty).ToLowerInvariant() ?? string.Empty;

            if (oldExtension != nowExtension)
            {
                document.RemoveField(nameof(CodeSource.FileExtension));
                document.Add(new StringField(nameof(CodeSource.FileExtension), nowExtension.ToLowerSafe(), Field.Store.YES));
            }

            var oldFileName = document.Get(nameof(CodeSource.FileName));
            if (oldFileName != fileInfo.Name)
            {
                document.RemoveField(nameof(CodeSource.FileName));
                document.Add(new TextField(nameof(CodeSource.FileName), fileInfo.Name.ToStringSafe(), Field.Store.YES));
            }

            return document;
        }

        public static Document RenameIndexForFolder(this Document document, string oldFolderPath, string nowFolderPath)
        {
            var pathField = document.Get(nameof(CodeSource.FilePath));
            var nowPath = pathField.Replace(oldFolderPath, nowFolderPath);
            document.RemoveField(nameof(CodeSource.FilePath));
            document.RemoveField(nameof(CodeSource.FilePath) + Constants.NoneTokenizeFieldSuffix);
            document.Add(new TextField(nameof(CodeSource.FilePath), nowPath.ToStringSafe(), Field.Store.YES));
            document.Add(new StringField(nameof(CodeSource.FilePath) + Constants.NoneTokenizeFieldSuffix, nowPath.ToStringSafe(), Field.Store.YES));

            return document;
        }
    }
}
