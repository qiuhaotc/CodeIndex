using System;
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

        public void DeleteIndex(Term term, out Document[] documentsBeenDeleted)
        {
            using var readLock = new EnterReaderWriterLock(readerWriteLock);
            using var searcher = GetUseIndexSearcher();
            documentsBeenDeleted = searcher.IndexSearcher.Search(new TermQuery(term), int.MaxValue).ScoreDocs.Select(hit => searcher.IndexSearcher.Doc(hit.Doc)).ToArray();
            IndexWriter.DeleteDocuments(term);

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
            using var searcher = GetUseIndexSearcher();
            Document[] documents;

            if (filter != null)
            {
                documents = searcher.IndexSearcher.Search(query, filter, maxResults).ScoreDocs.Select(hit => searcher.IndexSearcher.Doc(hit.Doc)).ToArray();
            }
            else
            {
                documents = searcher.IndexSearcher.Search(query, maxResults).ScoreDocs.Select(hit => searcher.IndexSearcher.Doc(hit.Doc)).ToArray();
            }

            return documents;
        }

        public Document[] SearchWithSpecificFields(Query query, int maxResults, params string[] fieldsNeedToLoad)
        {
            fieldsNeedToLoad.RequireContainsElement(nameof(fieldsNeedToLoad));

            using var readLock = new EnterReaderWriterLock(readerWriteLock);
            using var searcher = GetUseIndexSearcher();

            return searcher.IndexSearcher.Search(query, maxResults).ScoreDocs.Select(hit =>
            {
                var visitor = new DocumentStoredFieldVisitor(fieldsNeedToLoad);
                searcher.IndexSearcher.Doc(hit.Doc, visitor);
                return visitor.Document;
            }).ToArray();
        }

        public static Analyzer Analyzer => analyzer ??= CodeAnalyzer.GetCaseSensitiveAndInsesitiveCodeAnalyzer(CodeIndexBuilder.GetCaseSensitiveField(nameof(CodeSource.Content)));

        public static QueryParser GetQueryParser()
        {
            return new QueryParser(Constants.AppLuceneVersion, nameof(CodeSource.Content), Analyzer);
        }

        #endregion

        #region Fields

        readonly ReaderWriterLockSlim readerWriteLock = new ReaderWriterLockSlim();
        int indexChangeCount;
        bool isDisposing;
        static Analyzer analyzer;

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
                        if (indexWriter == null)
                        {
                            var dir = FSDirectory.Open(LuceneIndex);
                            //create an analyzer to process the text
                            //create an index writer
                            var indexConfig = new IndexWriterConfig(Constants.AppLuceneVersion, Analyzer);
                            indexWriter = new IndexWriter(dir, indexConfig);
                        }
                    }
                }

                return indexWriter;
            }
        }

        #endregion

        #region IndexSearcher

        readonly object syncLockForSearcher = new();
        IndexSearcher indexSearcher;

        UseIndexSearching GetUseIndexSearcher()
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
                return GetUseIndexSearcher(); // try get the IndexSearcher again
            }

            return new UseIndexSearching(indexSearcher);
        }

        public void DeleteAllIndex()
        {
            using var readLock = new EnterReaderWriterLock(readerWriteLock);

            IndexWriter.DeleteAll();
            IndexWriter.Commit();

            indexChangeCount++;
        }

        public void Commit()
        {
            using var readLock = new EnterReaderWriterLock(readerWriteLock);
            IndexWriter.Commit();
        }

        public void UpdateIndex(Term term, Document document)
        {
            using var readLock = new EnterReaderWriterLock(readerWriteLock);

            IndexWriter.UpdateDocument(term, document);
            indexChangeCount++;
        }

        public void UpdateIndex(Term term, Document document, out Document[] rawDocuments)
        {
            using var readLock = new EnterReaderWriterLock(readerWriteLock);
            using var searcher = GetUseIndexSearcher();

            rawDocuments = searcher.IndexSearcher.Search(new TermQuery(term), int.MaxValue).ScoreDocs.Select(hit => searcher.IndexSearcher.Doc(hit.Doc)).ToArray();
            IndexWriter.UpdateDocument(term, document);
            indexChangeCount++;
        }

        public bool Exists(Query query)
        {
            using var readLock = new EnterReaderWriterLock(readerWriteLock);
            using var searcher = GetUseIndexSearcher();
            return searcher.IndexSearcher.Search(query, 1).TotalHits == 1;
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

    class UseIndexSearching : IDisposable
    {
        public UseIndexSearching(IndexSearcher indexSearcher)
        {
            IndexSearcher = indexSearcher;
        }

        public IndexSearcher IndexSearcher { get; }

        public void Dispose()
        {
            IndexSearcher.IndexReader.DecRef(); // Dispose Safe
        }
    }
}
