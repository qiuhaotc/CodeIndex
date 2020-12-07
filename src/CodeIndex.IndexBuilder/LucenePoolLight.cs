using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using CodeIndex.Common;
using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;

namespace CodeIndex.IndexBuilder
{
    public class LucenePoolLight : ILucenePool
    {
        public LucenePoolLight(string luceneIndex)
        {
            luceneIndex.RequireNotNullOrEmpty(nameof(luceneIndex));
            LuceneIndex = luceneIndex;
        }

        #region ILucenePool

        public string LuceneIndex { get; }

        public void BuildIndex(IEnumerable<Document> documents, bool needCommit, bool triggerMerge = false, bool applyAllDeletes = false)
        {
            using var readLock = new EnterReaderWriterLock(readerWriteLock);
            IndexWriter.AddDocuments(documents);

            if (triggerMerge || applyAllDeletes)
            {
                IndexWriter.Flush(triggerMerge, applyAllDeletes);
            }

            if (needCommit)
            {
                IndexWriter.Commit();
            }

            indexChangeCount++;
        }

        public void DeleteIndex(params Query[] searchQueries)
        {
            using var readLock = new EnterReaderWriterLock(readerWriteLock);
            IndexWriter.DeleteDocuments(searchQueries);

            indexChangeCount++;
        }

        public void DeleteIndex(params Term[] terms)
        {
            using var readLock = new EnterReaderWriterLock(readerWriteLock);
            IndexWriter.DeleteDocuments(terms);

            indexChangeCount++;
        }

        public void Dispose()
        {
            using var readLock = new EnterReaderWriterLock(readerWriteLock, false);

            if (!isDisposing)
            {
                isDisposing = true;
                indexChangeCount = 0;
                indexReader?.Dispose();
                indexWriter?.Dispose();
            }
        }

        public Document[] Search(Query query, int maxResults, Filter filter = null)
        {
            using var readLock = new EnterReaderWriterLock(readerWriteLock);
            Document[] documents;
            var searcher = GetIndexSearcher();

            try
            {
                if (filter != null)
                {
                    documents = searcher.Search(query, filter, maxResults).ScoreDocs.Select(hit => searcher.Doc(hit.Doc)).ToArray();
                }
                else
                {
                    documents = searcher.Search(query, maxResults).ScoreDocs.Select(hit => searcher.Doc(hit.Doc)).ToArray();
                }
            }
            finally
            {
                searcher.IndexReader.DecRef(); // Dispose Safe
            }

            return documents;
        }

        public Analyzer Analyzer => analyzer ??= new CodeAnalyzer(Constants.AppLuceneVersion, true);

        #endregion

        #region Fields

        readonly ReaderWriterLockSlim readerWriteLock = new ReaderWriterLockSlim();
        int indexChangeCount;
        bool isDisposing;
        CodeAnalyzer analyzer;

        #endregion

        #region IndexWriter

        readonly object syncLockForWriter = new object();
        IndexWriter indexWriter;
        IndexWriter IndexWriter
        {
            get
            {
                if (indexWriter == null)
                {
                    lock (syncLockForWriter)
                    {
                        var dir = FSDirectory.Open(LuceneIndex);
                        //create an analyzer to process the text
                        //create an index writer
                        var indexConfig = new IndexWriterConfig(Constants.AppLuceneVersion, Analyzer);
                        indexWriter = new IndexWriter(dir, indexConfig);
                    }
                }

                return indexWriter;
            }
        }

        #endregion

        #region IndexSearcher

        readonly object syncLockForSearcher = new object();
        IndexSearcher indexSearcher;
        IndexSearcher GetIndexSearcher()
        {
            if (indexSearcher == null || indexChangeCount > 0)
            {
                lock (syncLockForSearcher)
                {
                    indexSearcher = new IndexSearcher(IndexReader);
                }
            }

            if (!indexSearcher.IndexReader.TryIncRef())
            {
                return GetIndexSearcher(); // try get the IndexSearcher again
            }

            return indexSearcher;
        }

        public void DeleteAllIndex()
        {
            using var readLock = new EnterReaderWriterLock(readerWriteLock);

            IndexWriter.DeleteAll();
            indexWriter.Commit();

            indexChangeCount++;
        }

        public void Commit()
        {
            using var readLock = new EnterReaderWriterLock(readerWriteLock);
            indexWriter.Commit();
        }

        #endregion

        #region IndexReader

        readonly object syncLockForReader = new object();
        IndexReader indexReader;
        IndexReader IndexReader
        {
            get
            {
                if (indexReader == null || indexChangeCount > 0)
                {
                    lock (syncLockForReader)
                    {
                        if (indexReader == null)
                        {
                            indexReader = IndexWriter.GetReader(true);
                        }
                        else
                        {
                            indexReader.DecRef(); // Dispose safely
                            indexReader = IndexWriter.GetReader(true);
                        }

                        indexChangeCount = 0;
                    }
                }

                return indexReader;
            }
        }

        #endregion
    }

    class EnterReaderWriterLock : IDisposable
    {
        public EnterReaderWriterLock(ReaderWriterLockSlim readerWriterLock, bool enterReadLock = true)
        {
            ReaderWriterLock = readerWriterLock;
            EnterReadLock = enterReadLock;

            if (EnterReadLock)
            {
                ReaderWriterLock.TryEnterReadLock(Constants.ReadWriteLockTimeOutMilliseconds);
            }
            else
            {
                ReaderWriterLock.TryEnterWriteLock(Constants.ReadWriteLockTimeOutMilliseconds);
            }
        }

        bool EnterReadLock { get; }

        ReaderWriterLockSlim ReaderWriterLock { get; }

        public void Dispose()
        {
            if (EnterReadLock)
            {
                ReaderWriterLock.ExitReadLock();
            }
            else
            {
                ReaderWriterLock.ExitWriteLock();
            }
        }
    }
}
