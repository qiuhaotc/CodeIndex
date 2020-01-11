using System;
using System.Collections.Concurrent;
using System.Linq;
using CodeIndex.Common;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;

namespace CodeIndex.IndexBuilder
{
    public static class CodeIndexSearcher
    {
        public static ConcurrentDictionary<string, DirectoryReader> DirectoryReadersPool { get; set; } = new ConcurrentDictionary<string, DirectoryReader>();

        public static Document[] Search(string path, Query query, int maxResults)
        {
            path.RequireNotNullOrEmpty(nameof(path));
            query.RequireNotNull(nameof(query));
            maxResults.RequireRange(nameof(maxResults), int.MaxValue, 1);

            if(!DirectoryReadersPool.TryGetValue(path, out var reader))
            {
                reader = DirectoryReader.Open(FSDirectory.Open(path));
                DirectoryReadersPool.TryAdd(path, reader);
            }

            var searcher = new IndexSearcher(reader);
            var hits = searcher.Search(query, maxResults).ScoreDocs;
            return hits.Select(hit => searcher.Doc(hit.Doc)).ToArray();
        }

        public static void ClearDirectoryReadersPool()
        {
            foreach (var item in DirectoryReadersPool)
            {
                item.Value.Dispose();
            }

            DirectoryReadersPool.Clear();
        }
    }
}
