using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using CodeIndex.Common;
using CodeIndex.LuceneContainer;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.Standard;
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
                readWriteLock.AcquireReaderLock(Constants.ReadWriteLockTimeOutMilliseconds);

                var writer = CreateOrGetIndexWriter(luceneIndex);
                writer.AddDocuments(documents);

                if (needFlush)
                {
                    writer.Flush(triggerMerge, applyAllDeletes);
                }
            }
            finally
            {
                readWriteLock.ReleaseReaderLock();
            }
        }

        internal static void DeleteIndex(string luceneIndex, params Query[] searchQueries)
        {
            try
            {
                readWriteLock.AcquireReaderLock(Constants.ReadWriteLockTimeOutMilliseconds);

                var indexWriter = CreateOrGetIndexWriter(luceneIndex);
                indexWriter.DeleteDocuments(searchQueries);
            }
            finally
            {
                readWriteLock.ReleaseReaderLock();
            }
        }

        internal static void DeleteIndex(string luceneIndex, params Term[] terms)
        {
            try
            {
                readWriteLock.AcquireReaderLock(Constants.ReadWriteLockTimeOutMilliseconds);

                var indexWriter = CreateOrGetIndexWriter(luceneIndex);
                indexWriter.DeleteDocuments(terms);
            }
            finally
            {
                readWriteLock.ReleaseReaderLock();
            }
        }

        internal static void UpdateIndex(string luceneIndex, Term term, Document document)
        {
            try
            {
                readWriteLock.AcquireReaderLock(Constants.ReadWriteLockTimeOutMilliseconds);

                var indexWriter = CreateOrGetIndexWriter(luceneIndex);
                indexWriter.UpdateDocument(term, document);
            }
            finally
            {
                readWriteLock.ReleaseReaderLock();
            }
        }

        internal static void DeleteAllIndex(string luceneIndex)
        {
            try
            {
                readWriteLock.AcquireReaderLock(Constants.ReadWriteLockTimeOutMilliseconds);

                var indexWriter = CreateOrGetIndexWriter(luceneIndex);
                indexWriter.DeleteAll();
                indexWriter.Commit();
            }
            finally
            {
                readWriteLock.ReleaseReaderLock();
            }
        }

        public static void SaveLuceneResultsAndCloseIndexWriter(string luceneIndex)
        {
            try
            {
                readWriteLock.AcquireWriterLock(Constants.ReadWriteLockTimeOutMilliseconds);

                if (IndexWritesPool.TryRemove(luceneIndex, out var indexWriter))
                {
                    indexWriter.Dispose();
                }
            }
            finally
            {
                readWriteLock.ReleaseWriterLock();
            }
        }

        static object syncLock = new object();

        static IndexWriter CreateOrGetIndexWriter(string luceneIndex)
        {
            IndexWriter indexWriter;

            if (!IndexWritesPool.TryGetValue(luceneIndex, out indexWriter))
            {
                lock (syncLock)
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

        public static void ClearIndexWritesPool()
        {
            foreach (var item in IndexWritesPool)
            {
                item.Value.Dispose();
            }

            IndexWritesPool.Clear();
        }

        public static ConcurrentDictionary<string, IndexWriter> IndexWritesPool { get; set; } = new ConcurrentDictionary<string, IndexWriter>();

        public static QueryParser GetQueryParser()
        {
            return new QueryParser(Constants.AppLuceneVersion, nameof(CodeSource.Content), GetAnalyzer());
        }

        static readonly ReaderWriterLock readWriteLock = new ReaderWriterLock();

        public static Analyzer GetAnalyzer()
        {
            return new StandardAnalyzer(Constants.AppLuceneVersion);
        }
    }
}
