using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Web;
using CodeIndex.Common;
using CodeIndex.IndexBuilder;
using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Search.Highlight;
using Lucene.Net.Store;

namespace CodeIndex.Search
{
    public static class CodeIndexSearcher
    {
        // TODO: Move search logic to Lucene Pool

        public static ConcurrentDictionary<string, DirectoryReader> DirectoryReadersPool { get; } = new ConcurrentDictionary<string, DirectoryReader>();

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

        public static string GenerateHtmlPreviewText(Query query, string text, int length, Analyzer analyzer, string prefix = "<label class='highlight'>", string suffix = "</label>", bool returnRawContentWhenResultIsEmpty = false)
        {
            string result = null;

            if(query != null)
            {
                var scorer = new QueryScorer(query);
                var formatter = new SimpleHTMLFormatter(CodeContentProcessing.HighLightPrefix, CodeContentProcessing.HighLightSuffix);

                var highlighter = new Highlighter(formatter, scorer);
                highlighter.TextFragmenter = new SimpleFragmenter(length);
                highlighter.MaxDocCharsToAnalyze = int.MaxValue;

                var stream = analyzer.GetTokenStream(nameof(CodeSource.Content), new StringReader(text));

                result = highlighter.GetBestFragments(stream, text, 3, "...");
            }

            result = string.IsNullOrEmpty(result) ?
                    (returnRawContentWhenResultIsEmpty ? HttpUtility.HtmlEncode(text) : string.Empty)
                    : HttpUtility.HtmlEncode(result).Replace(CodeContentProcessing.HighLightPrefix, prefix).Replace(CodeContentProcessing.HighLightSuffix, suffix);

            return result;
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
