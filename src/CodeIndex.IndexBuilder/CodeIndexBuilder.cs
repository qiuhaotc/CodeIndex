using System.Collections.Generic;
using CodeIndex.Common;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;

namespace CodeIndex.IndexBuilder
{
    public static class CodeIndexBuilder
    {
        public static void BuildIndex(string luceneIndex, bool triggerMerge, bool applyAllDeletes, bool needFlush, IEnumerable<CodeSource> codeSources)
        {
            luceneIndex.RequireNotNullOrEmpty(nameof(luceneIndex));
            codeSources.RequireNotNull(nameof(codeSources));
            var indexExist = IndexExists(luceneIndex);
            var documents = new List<Document>();
            foreach (var source in codeSources)
            {
                if (indexExist)
                {
                    DeleteIndex(luceneIndex, new Term(nameof(CodeSource.FilePath), source.FilePath));
                }

                var doc = GetDocumentFromSource(source);
                documents.Add(doc);

                var words = GetWords(source.Content);

                foreach (var word in words)
                {
                    documents.Add(new Document
                    {
                        new StringField("Word", word, Field.Store.YES),
                        new StringField("WordLower", word.ToLower(), Field.Store.YES),
                    });
                }
            }

            LucenePool.BuildIndex(luceneIndex, triggerMerge, applyAllDeletes, documents, needFlush);
        }

        static string[] GetWords(string content)
        {
            var words = new List<string>();
            var chars = new List<char>();

            foreach (var ch in content)
            {
                if (!WordSegmenter.IsSpecialChar(ch) && !WordSegmenter.SpaceLike(ch))
                {
                    chars.Add(ch);
                }
                else if (chars.Count > 0)
                {
                    words.Add(new string(chars.ToArray()));
                    chars.Clear();
                }
            }

            if (chars.Count > 0)
            {
                words.Add(new string(chars.ToArray()));
            }

            return words.ToArray();
        }

        public static void DeleteIndex(string luceneIndex, params Query[] searchQueries)
        {
            luceneIndex.RequireNotNullOrEmpty(nameof(luceneIndex));
            searchQueries.RequireContainsElement(nameof(searchQueries));

            LucenePool.DeleteIndex(luceneIndex, searchQueries);
        }

        public static void DeleteIndex(string luceneIndex, params Term[] terms)
        {
            luceneIndex.RequireNotNullOrEmpty(nameof(luceneIndex));
            terms.RequireContainsElement(nameof(terms));

            LucenePool.DeleteIndex(luceneIndex, terms);
        }

        public static void UpdateIndex(string luceneIndex, Term term, Document document)
        {
            luceneIndex.RequireNotNullOrEmpty(nameof(luceneIndex));
            term.RequireNotNull(nameof(term));
            document.RequireNotNull(nameof(document));

            LucenePool.UpdateIndex(luceneIndex, term, document);
        }

        public static bool IndexExists(string luceneIndex)
        {
            luceneIndex.RequireNotNullOrEmpty(nameof(luceneIndex));

            return DirectoryReader.IndexExists(FSDirectory.Open(luceneIndex));
        }

        public static void DeleteAllIndex(string luceneIndex)
        {
            luceneIndex.RequireNotNullOrEmpty(nameof(luceneIndex));

            if (IndexExists(luceneIndex))
            {
                LucenePool.DeleteAllIndex(luceneIndex);
            }
        }

        static string ToStringSafe(this string value)
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
                new Int64Field(nameof(source.IndexDate), source.IndexDate.Ticks, Field.Store.YES),
                new Int64Field(nameof(source.LastWriteTimeUtc), source.LastWriteTimeUtc.Ticks, Field.Store.YES),
                new StringField(nameof(source.CodePK), source.CodePK.ToString(), Field.Store.YES)
            };
        }

        public static void UpdateCodeFilePath(Document codeSourceDocumnet, string oldFullPath, string nowFullPath)
        {
            var pathField = codeSourceDocumnet.Get(nameof(CodeSource.FilePath));
            codeSourceDocumnet.RemoveField(nameof(CodeSource.FilePath));
            codeSourceDocumnet.RemoveField(nameof(CodeSource.FilePath) + Constants.NoneTokenizeFieldSuffix);
            codeSourceDocumnet.Add(new TextField(nameof(CodeSource.FilePath), pathField.Replace(oldFullPath, nowFullPath), Field.Store.YES));
            codeSourceDocumnet.Add(new StringField(nameof(CodeSource.FilePath) + Constants.NoneTokenizeFieldSuffix, pathField.Replace(oldFullPath, nowFullPath), Field.Store.YES));
        }

        public static Document GetDocument(string luceneIndex, Term term)
        {
            luceneIndex.RequireNotNullOrEmpty(nameof(luceneIndex));
            term.RequireNotNull(nameof(term));

            var document = LucenePool.GetDocument(luceneIndex, term);
            return document;
        }
    }
}
