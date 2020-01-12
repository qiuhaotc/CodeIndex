using System;
using System.Collections.Concurrent;
using CodeIndex.Common;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using static CodeIndex.Common.ArgumentValidation;

namespace CodeIndex.IndexBuilder
{
    public static class CodeIndexBuilder
    {
        public const LuceneVersion AppLuceneVersion = LuceneVersion.LUCENE_48;
        public static ConcurrentDictionary<string, IndexWriter> IndexWritesPool { get; set; } = new ConcurrentDictionary<string, IndexWriter>();

        public static void BuildIndex(string luceneIndex, bool triggerMerge, bool applyAllDeletes, params CodeSource[] codeSources)
        {
            luceneIndex.RequireNotNullOrEmpty(nameof(luceneIndex));
            codeSources.RequireContainsElement(nameof(codeSources));
            var indexWriter = CreateOrGetIndexWriter(luceneIndex);

            foreach (var source in codeSources)
            {
                var doc = GetDocumentFromSource(source);
                indexWriter.AddDocument(doc);
            }

            indexWriter.Flush(triggerMerge: triggerMerge, applyAllDeletes: applyAllDeletes);
        }

        public static void DeleteIndex(string luceneIndex, params Query[] searchQueries)
        {
            luceneIndex.RequireNotNullOrEmpty(nameof(luceneIndex));
            searchQueries.RequireContainsElement(nameof(searchQueries));

            var indexWriter = CreateOrGetIndexWriter(luceneIndex);
            indexWriter.DeleteDocuments(searchQueries);
        }

        public static void DeleteIndex(string luceneIndex, params Term[] terms)
        {
            luceneIndex.RequireNotNullOrEmpty(nameof(luceneIndex));
            terms.RequireContainsElement(nameof(terms));

            var indexWriter = CreateOrGetIndexWriter(luceneIndex);
            indexWriter.DeleteDocuments(terms);
        }

        public static void UpdateIndex(string luceneIndex, Term term, CodeSource codeSources)
        {
            luceneIndex.RequireNotNullOrEmpty(nameof(luceneIndex));
            term.RequireNotNull(nameof(term));
            codeSources.RequireNotNull(nameof(codeSources));

            var indexWriter = CreateOrGetIndexWriter(luceneIndex);
            indexWriter.UpdateDocument(term, GetDocumentFromSource(codeSources));
        }

        public static void CloseIndexWriterAndCommitChange(string luceneIndex)
        {
            if (IndexWritesPool.TryRemove(luceneIndex, out var indexWriter))
            {
                indexWriter.Dispose();
            }
        }

        public static void ClearIndexWritesPool()
        {
            foreach (var item in IndexWritesPool)
            {
                item.Value.Dispose();
            }

            IndexWritesPool.Clear();
        }

        public static IndexWriter CreateOrGetIndexWriter(string luceneIndex)
        {
            IndexWriter indexWriter;

            if (!IndexWritesPool.TryGetValue(luceneIndex, out indexWriter))
            {
                var dir = FSDirectory.Open(luceneIndex);
                //create an analyzer to process the text
                var analyzer = new StandardAnalyzer(AppLuceneVersion);
                //create an index writer
                var indexConfig = new IndexWriterConfig(AppLuceneVersion, analyzer);

                indexWriter = new IndexWriter(dir, indexConfig);
                IndexWritesPool.TryAdd(luceneIndex, indexWriter);
            }

            return indexWriter;
        }

        static string ToStringSafe(this string value)
        {
            return value?.ToString() ?? string.Empty;
        }

        static Document GetDocumentFromSource(CodeSource source)
        {
            return new Document
            {
                // StringField indexes but doesn't tokenize
                new StringField(nameof(source.FileName), source.FileName.ToStringSafe(), Field.Store.YES),
                new StringField(nameof(source.FileExtension), source.FileExtension.ToStringSafe(), Field.Store.YES),
                new StringField(nameof(source.FilePath), source.FilePath.ToStringSafe(), Field.Store.YES),
                new TextField(nameof(source.Content), source.Content.ToStringSafe(), Field.Store.YES),
                new Int64Field(nameof(source.IndexDate), source.IndexDate.Ticks, Field.Store.YES)
            };
        }
    }
}
