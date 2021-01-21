using System;
using System.Collections.Generic;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;

namespace CodeIndex.IndexBuilder
{
    public interface ILucenePool : IDisposable
    {
        void BuildIndex(IEnumerable<Document> documents, bool needCommit, bool triggerMerge = false, bool applyAllDeletes = false);

        Document[] Search(Query query, int maxResults, Filter filter = null);

        Document[] SearchWithSpecificFields(Query query, int maxResults, params string[] fieldsNeedToLoad);

        void DeleteIndex(params Query[] searchQueries);

        void DeleteIndex(params Term[] terms);

        void DeleteIndex(Term term, out Document[] documentsBeenDeleted);

        void DeleteIndex(Query query, out Document[] documentsBeenDeleted);

        void UpdateIndex(Term term, Document document);

        void UpdateIndex(Term term, Document document, out Document[] rawDocuments);

        void DeleteAllIndex();

        bool Exists(Query query);

        string LuceneIndex { get; }
    }
}
