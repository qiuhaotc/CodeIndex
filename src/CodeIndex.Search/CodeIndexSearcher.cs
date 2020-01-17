using System;
using System.Collections.Concurrent;
using System.Linq;
using CodeIndex.Common;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;

namespace CodeIndex.Search
{
    public static class CodeIndexSearcher
    {
        public static ConcurrentDictionary<string, DirectoryReader> DirectoryReadersPool { get; set; } = new ConcurrentDictionary<string, DirectoryReader>();

        public static Document[] Search(string path, DirectoryReader reader, Query query, int maxResults)
        {
            path.RequireNotNullOrEmpty(nameof(path));
            reader.RequireNotNull(nameof(reader));
            query.RequireNotNull(nameof(query));
            maxResults.RequireRange(nameof(maxResults), int.MaxValue, 1);

            var searcher = new IndexSearcher(reader);
            var hits = searcher.Search(query, maxResults).ScoreDocs;
            return hits.Select(hit => searcher.Doc(hit.Doc)).ToArray();
        }

        public static CodeSource[] SearchCode(string path, DirectoryReader reader, Query query, int maxResults)
        {
            path.RequireNotNullOrEmpty(nameof(path));
            reader.RequireNotNull(nameof(reader));
            query.RequireNotNull(nameof(query));
            maxResults.RequireRange(nameof(maxResults), int.MaxValue, 1);

            var searcher = new IndexSearcher(reader);
            var hits = searcher.Search(query, maxResults).ScoreDocs;
            return hits.Select(hit => GetCodeSourceFromDocumet(searcher.Doc(hit.Doc))).ToArray();
        }

        public static Document[] Search(string path, Query query, int maxResults)
        {
            path.RequireNotNullOrEmpty(nameof(path));
            query.RequireNotNull(nameof(query));
            maxResults.RequireRange(nameof(maxResults), int.MaxValue, 1);

            if (!DirectoryReadersPool.TryGetValue(path, out var reader))
            {
                reader = DirectoryReader.Open(FSDirectory.Open(path));
                DirectoryReadersPool.TryAdd(path, reader);
            }
            else
            {
                var tempReader = DirectoryReader.OpenIfChanged(reader);
                if (tempReader != null)
                {
                    reader.Dispose();
                    DirectoryReadersPool.TryUpdate(path, tempReader, reader);
                    reader = tempReader;
                }
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

        public static CodeSource GetCodeSourceFromDocumet(Document document)
        {
            return new CodeSource
            {
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
