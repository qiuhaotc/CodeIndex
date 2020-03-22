using System.IO;
using System.Web;
using CodeIndex.Common;
using CodeIndex.IndexBuilder;
using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Search;
using Lucene.Net.Search.Highlight;

namespace CodeIndex.Search
{
    public static class CodeIndexSearcher
    {
        public static CodeSource[] SearchCode(string luceneIndex, Query query, int maxResults)
        {
            luceneIndex.RequireNotNullOrEmpty(nameof(luceneIndex));
            query.RequireNotNull(nameof(query));
            maxResults.RequireRange(nameof(maxResults), int.MaxValue, 1);

            return LucenePool.SearchCode(luceneIndex, query, maxResults);
        }

        public static string GenerateHtmlPreviewText(Query query, string text, int length, Analyzer analyzer, string prefix = "<label class='highlight'>", string suffix = "</label>", bool returnRawContentWhenResultIsEmpty = false, int maxContentHighlightLength = Constants.DefaultMaxContentHighlightLength)
        {
            string result = null;

            if (text.Length <= maxContentHighlightLength) // For performance
            {
                if (query != null)
                {
                    var scorer = new QueryScorer(query);
                    var formatter = new SimpleHTMLFormatter(CodeContentProcessing.HighLightPrefix, CodeContentProcessing.HighLightSuffix);

                    var highlighter = new Highlighter(formatter, scorer);
                    highlighter.TextFragmenter = new SimpleFragmenter(length);
                    highlighter.MaxDocCharsToAnalyze = maxContentHighlightLength;

                    var stream = analyzer.GetTokenStream(nameof(CodeSource.Content), new StringReader(text));

                    result = highlighter.GetBestFragments(stream, text, 3, "...");
                }

                result = string.IsNullOrEmpty(result) ?
                        (returnRawContentWhenResultIsEmpty ? HttpUtility.HtmlEncode(text) : string.Empty)
                        : HttpUtility.HtmlEncode(result).Replace(CodeContentProcessing.HighLightPrefix, prefix).Replace(CodeContentProcessing.HighLightSuffix, suffix);
            }
            else
            {
                result = "Content is too long to highlight";
            }

            return result;
        }

        public static Document[] Search(string luceneIndex, Query query, int maxResults)
        {
            luceneIndex.RequireNotNullOrEmpty(nameof(luceneIndex));
            query.RequireNotNull(nameof(query));
            maxResults.RequireRange(nameof(maxResults), int.MaxValue, 1);

            return LucenePool.Search(luceneIndex, query, maxResults);
        }

        public static string[] GetHints(string luceneIndex, string word, int maxResults = 20, bool caseSensitive = false)
        {
            return LucenePool.GetHints(luceneIndex, word, maxResults, caseSensitive);
        }
    }
}
