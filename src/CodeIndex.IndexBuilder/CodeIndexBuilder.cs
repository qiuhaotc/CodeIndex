using System;
using System.Collections.Concurrent;
using CodeIndex.Common;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using static CodeIndex.Common.ArgumentValidation;

namespace CodeIndex.IndexBuilder
{
    public static class CodeIndexBuilder
    {
        public static ConcurrentDictionary<string, IndexWriter> IndexWritesPool { get; set; } = new ConcurrentDictionary<string, IndexWriter>();

        public static void BuildIndex(string luceneIndex, bool triggerMerge, bool applyAllDeletes, params CodeSource[] codeSources)
        {
            luceneIndex.RequireNotNullOrEmpty(nameof(luceneIndex));
            codeSources.RequireContainsElement(nameof(codeSources));

            if(!IndexWritesPool.TryGetValue(luceneIndex, out var indexWriter))
            {
                var AppLuceneVersion = LuceneVersion.LUCENE_48;

                var indexLocation = luceneIndex;
                var dir = FSDirectory.Open(indexLocation);

                //create an analyzer to process the text
                var analyzer = new StandardAnalyzer(AppLuceneVersion);

                //create an index writer
                var indexConfig = new IndexWriterConfig(AppLuceneVersion, analyzer);
                indexWriter = new IndexWriter(dir, indexConfig);

                IndexWritesPool.TryAdd(luceneIndex, indexWriter);
            }

            foreach (var source in codeSources)
            {
                var doc = new Document
                {
                    // StringField indexes but doesn't tokenize
                    new StringField(nameof(source.FileName), source.FileName.ToStringSafe(), Field.Store.YES),
                    new StringField(nameof(source.FileExtension), source.FileExtension.ToStringSafe(), Field.Store.YES),
                    new StringField(nameof(source.FilePath), source.FilePath.ToStringSafe(), Field.Store.YES),
                    new TextField(nameof(source.Content), source.Content.ToStringSafe(), Field.Store.YES),
                };

                indexWriter.AddDocument(doc);
            }

            indexWriter.Flush(triggerMerge: triggerMerge, applyAllDeletes: applyAllDeletes);
        }

        public static void CloseIndexWriter(string luceneIndex)
        {
            if (IndexWritesPool.TryRemove(luceneIndex, out var indexWriter))
            {
                indexWriter.Dispose();
            }
        }

        public static void ClearIndexWritesPool()
        {
            foreach(var item in IndexWritesPool)
            {
                item.Value.Dispose();
            }

            IndexWritesPool.Clear();
        }

        static string ToStringSafe(this string value)
        {
            return value?.ToString() ?? string.Empty;
        }
    }
}
