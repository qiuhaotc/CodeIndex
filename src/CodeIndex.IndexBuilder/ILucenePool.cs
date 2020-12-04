using System;
using System.Collections.Generic;
using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;

namespace CodeIndex.IndexBuilder
{
    public interface ILucenePool : IDisposable
    {
        void BuildIndex(IEnumerable<Document> documents, bool needCommit, bool triggerMerge = false, bool applyAllDeletes = false);

        Document[] Search(Query query, int maxResults, Filter filter = null);

        void DeleteIndex(params Query[] searchQueries);

        void DeleteIndex(params Term[] terms);

        string LuceneIndex { get; }

        Analyzer Analyzer { get; }
    }
}
