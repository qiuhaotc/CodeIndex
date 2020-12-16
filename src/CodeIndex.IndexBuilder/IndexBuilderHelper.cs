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

        public static Document GetDocumentFromSource(CodeSource source)
        {
            return new Document
            {
                new TextField(nameof(source.FileName), source.FileName.ToStringSafe(), Field.Store.YES),
                // StringField indexes but doesn't tokenize
                new StringField(nameof(source.FileExtension), source.FileExtension.ToStringSafe(), Field.Store.YES),
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
    }
}
