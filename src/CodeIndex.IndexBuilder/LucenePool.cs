using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using CodeIndex.Common;
using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Search;
using Lucene.Net.Store;

namespace CodeIndex.IndexBuilder
{
    public class LucenePool
    {
        internal static void BuildIndex(string luceneIndex, bool triggerMerge, bool applyAllDeletes, IEnumerable<Document> documents, bool needFlush)
        {
            try
            {
                readWriteLock.TryEnterReadLock(Constants.ReadWriteLockTimeOutMilliseconds);

                var writer = CreateOrGetIndexWriter(luceneIndex);
                writer.AddDocuments(documents);

                if (needFlush)
                {
                    writer.Flush(triggerMerge, applyAllDeletes);
                }

                IndexGotChanged.AddOrUpdate(luceneIndex, u => 0, (u, v) => v + 1);
            }
            finally
            {
                readWriteLock.ExitReadLock();
            }
        }

        internal static List<(string FilePath, DateTime LastWriteTimeUtc)> GetAllIndexedCodeSource(string luceneIndex)
        {
            try
            {
                readWriteLock.TryEnterReadLock(Constants.ReadWriteLockTimeOutMilliseconds);

                var query = new MatchAllDocsQuery();
                var filter = new FieldValueFilter(nameof(CodeSource.FilePath));

                return SearchDocuments(luceneIndex, query, int.MaxValue, filter).Select(u => (u.Get(nameof(CodeSource.FilePath)), new DateTime(long.Parse(u.Get(nameof(CodeSource.LastWriteTimeUtc)))))).ToList();
            }
            finally
            {
                readWriteLock.ExitReadLock();
            }
        }

        internal static void DeleteIndex(string luceneIndex, params Query[] searchQueries)
        {
            try
            {
                readWriteLock.TryEnterReadLock(Constants.ReadWriteLockTimeOutMilliseconds);

                var indexWriter = CreateOrGetIndexWriter(luceneIndex);
                indexWriter.DeleteDocuments(searchQueries);

                IndexGotChanged.AddOrUpdate(luceneIndex, u => 0, (u, v) => v + 1);
            }
            finally
            {
                readWriteLock.ExitReadLock();
            }
        }

        internal static void DeleteIndex(string luceneIndex, params Term[] terms)
        {
            try
            {
                readWriteLock.TryEnterReadLock(Constants.ReadWriteLockTimeOutMilliseconds);

                var indexWriter = CreateOrGetIndexWriter(luceneIndex);
                indexWriter.DeleteDocuments(terms);

                IndexGotChanged.AddOrUpdate(luceneIndex, u => 0, (u, v) => v + 1);
            }
            finally
            {
                readWriteLock.ExitReadLock();
            }
        }

        internal static void UpdateIndex(string luceneIndex, Term term, Document document)
        {
            try
            {
                readWriteLock.TryEnterReadLock(Constants.ReadWriteLockTimeOutMilliseconds);

                var indexWriter = CreateOrGetIndexWriter(luceneIndex);
                indexWriter.UpdateDocument(term, document);

                IndexGotChanged.AddOrUpdate(luceneIndex, u => 0, (u, v) => v + 1);
            }
            finally
            {
                readWriteLock.ExitReadLock();
            }
        }

        internal static void DeleteAllIndex(string luceneIndex)
        {
            try
            {
                readWriteLock.TryEnterReadLock(Constants.ReadWriteLockTimeOutMilliseconds);

                var indexWriter = CreateOrGetIndexWriter(luceneIndex);
                indexWriter.DeleteAll();
                indexWriter.Commit();

                IndexGotChanged.AddOrUpdate(luceneIndex, u => 0, (u, v) => v + 1);
            }
            finally
            {
                readWriteLock.ExitReadLock();
            }
        }

        public static void SaveResultsAndClearLucenePool(CodeIndexConfiguration config)
        {
            SaveResultsAndClearLucenePool(config.LuceneIndexForCode);
            SaveResultsAndClearLucenePool(config.LuceneIndexForHint);
        }

        public static void SaveResultsAndClearLucenePool(string luceneIndex)
        {
            try
            {
                readWriteLock.TryEnterWriteLock(Constants.ReadWriteLockTimeOutMilliseconds);

                if (IndexReaderPool.TryRemove(luceneIndex, out var indexReader))
                {
                    indexReader.Dispose();
                }

                if (IndexWritesPool.TryRemove(luceneIndex, out var indexWriter))
                {
                    indexWriter.Dispose();
                }

                IndexSearcherPool.TryRemove(luceneIndex, out _);

                IndexGotChanged.AddOrUpdate(luceneIndex, u => 0, (u, v) => 0);
            }
            finally
            {
                readWriteLock.ExitWriteLock();
            }
        }

        static readonly object syncLockForWriter = new object();

        static IndexWriter CreateOrGetIndexWriter(string luceneIndex)
        {
            IndexWriter indexWriter;

            if (!IndexWritesPool.TryGetValue(luceneIndex, out indexWriter))
            {
                lock (syncLockForWriter)
                {
                    if (!IndexWritesPool.TryGetValue(luceneIndex, out indexWriter))
                    {
                        var dir = FSDirectory.Open(luceneIndex);
                        //create an analyzer to process the text
                        //create an index writer
                        var indexConfig = new IndexWriterConfig(Constants.AppLuceneVersion, GetAnalyzer());

                        indexWriter = new IndexWriter(dir, indexConfig);
                        IndexWritesPool.TryAdd(luceneIndex, indexWriter);
                    }
                }
            }

            return indexWriter;
        }

        static readonly object syncLockForSearcher = new object();

        static IndexSearcher CreateOrGetIndexSearcher(string luceneIndex)
        {
            IndexSearcher indexSearcher;

            if (!IndexSearcherPool.TryGetValue(luceneIndex, out indexSearcher) || IndexGotChanged.TryGetValue(luceneIndex, out var indexChangedTimes) && indexChangedTimes > 0)
            {
                lock (syncLockForSearcher)
                {
                    if (!IndexSearcherPool.TryGetValue(luceneIndex, out indexSearcher))
                    {
                        indexSearcher = new IndexSearcher(CreateOrGetIndexReader(luceneIndex, false));
                        IndexSearcherPool.TryAdd(luceneIndex, indexSearcher);
                    }
                    else if (IndexGotChanged.TryGetValue(luceneIndex, out indexChangedTimes) && indexChangedTimes > 0)
                    {
                        indexSearcher = new IndexSearcher(CreateOrGetIndexReader(luceneIndex, true));
                        IndexSearcherPool.AddOrUpdate(luceneIndex, indexSearcher, (u, v) => indexSearcher);
                        IndexGotChanged.AddOrUpdate(luceneIndex, 0, (u, v) => 0);
                    }
                }
            }

            if (!indexSearcher.IndexReader.TryIncRef())
            {
                return CreateOrGetIndexSearcher(luceneIndex);
            }

            return indexSearcher;
        }

        static Document[] SearchDocuments(string luceneIndex, Query query, int maxResult, FieldValueFilter fieldValueFilter = null)
        {
            Document[] documents = null;
            IndexSearcher indexSearcher = null;

            try
            {
                indexSearcher = CreateOrGetIndexSearcher(luceneIndex);

                if (fieldValueFilter != null)
                {
                    documents = indexSearcher.Search(query, fieldValueFilter, maxResult).ScoreDocs.Select(hit => indexSearcher.Doc(hit.Doc)).ToArray();
                }
                else
                {
                    documents = indexSearcher.Search(query, maxResult).ScoreDocs.Select(hit => indexSearcher.Doc(hit.Doc)).ToArray();
                }
            }
            finally
            {
                indexSearcher.IndexReader.DecRef();
            }

            return documents ?? Array.Empty<Document>();
        }

        static readonly object syncLockForReader = new object();

        static IndexReader CreateOrGetIndexReader(string luceneIndex, bool forceRefresh)
        {
            IndexReader indexReader;

            if (!IndexReaderPool.TryGetValue(luceneIndex, out indexReader) || forceRefresh)
            {
                lock (syncLockForReader)
                {
                    if (!IndexReaderPool.TryGetValue(luceneIndex, out indexReader))
                    {
                        indexReader = CreateOrGetIndexWriter(luceneIndex).GetReader(true);
                        IndexReaderPool.TryAdd(luceneIndex, indexReader);
                    }
                    else if (forceRefresh)
                    {
                        indexReader.DecRef(); // Dispose safely
                        indexReader = CreateOrGetIndexWriter(luceneIndex).GetReader(true);
                        IndexReaderPool.AddOrUpdate(luceneIndex, indexReader, (u, v) => indexReader);
                    }
                }
            }

            return indexReader;
        }

        public static Document[] Search(string luceneIndex, Query query, int maxResults)
        {
            try
            {
                readWriteLock.TryEnterReadLock(Constants.ReadWriteLockTimeOutMilliseconds);

                return SearchDocuments(luceneIndex, query, maxResults);
            }
            finally
            {
                readWriteLock.ExitReadLock();
            }
        }

        public static string[] GetHints(string luceneIndex, string word, int maxResults, bool caseSensitive)
        {
            PrefixQuery query;

            try
            {
                readWriteLock.TryEnterReadLock(Constants.ReadWriteLockTimeOutMilliseconds);

                if (caseSensitive)
                {
                    query = new PrefixQuery(new Term(nameof(CodeWord.Word), word));
                }
                else
                {
                    query = new PrefixQuery(new Term(nameof(CodeWord.WordLower), word.ToLower()));
                }

                return SearchDocuments(luceneIndex, query, maxResults).Select(u => u.Get(nameof(CodeWord.Word))).ToArray();
            }
            finally
            {
                readWriteLock.ExitReadLock();
            }
        }

        public static CodeSource[] SearchCode(string luceneIndex, Query query, int maxResults)
        {
            try
            {
                readWriteLock.TryEnterReadLock(Constants.ReadWriteLockTimeOutMilliseconds);

                return SearchDocuments(luceneIndex, query, maxResults).Select(doc => GetCodeSourceFromDocument(doc)).ToArray();
            }
            finally
            {
                readWriteLock.ExitReadLock();
            }
        }

        public static ConcurrentDictionary<string, IndexWriter> IndexWritesPool { get; } = new ConcurrentDictionary<string, IndexWriter>();
        static ConcurrentDictionary<string, IndexSearcher> IndexSearcherPool { get; } = new ConcurrentDictionary<string, IndexSearcher>();
        static ConcurrentDictionary<string, int> IndexGotChanged { get; } = new ConcurrentDictionary<string, int>();
        static ConcurrentDictionary<string, IndexReader> IndexReaderPool { get; } = new ConcurrentDictionary<string, IndexReader>();

        public static QueryParser GetQueryParser()
        {
            return new QueryParser(Constants.AppLuceneVersion, nameof(CodeSource.Content), GetAnalyzer());
        }

        static readonly ReaderWriterLockSlim readWriteLock = new ReaderWriterLockSlim();

        public static Analyzer GetAnalyzer()
        {
            return new CodeAnalyzer(Constants.AppLuceneVersion, true);
        }

        public static CodeSource GetCodeSourceFromDocument(Document document)
        {
            return new CodeSource
            {
                CodePK = document.Get(nameof(CodeSource.CodePK)),
                Content = document.Get(nameof(CodeSource.Content)),
                FileExtension = document.Get(nameof(CodeSource.FileExtension)),
                FileName = document.Get(nameof(CodeSource.FileName)),
                FilePath = document.Get(nameof(CodeSource.FilePath)),
                IndexDate = new DateTime(long.Parse(document.Get(nameof(CodeSource.IndexDate)))),
                LastWriteTimeUtc = new DateTime(long.Parse(document.Get(nameof(CodeSource.LastWriteTimeUtc))))
            };
        }
    }
}
