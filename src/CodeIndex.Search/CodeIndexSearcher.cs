﻿using System.Collections.Generic;
using System.IO;
using System.Web;
using CodeIndex.Common;
using CodeIndex.IndexBuilder;
using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Search;
using Lucene.Net.Search.Highlight;
using static CodeIndex.IndexBuilder.CodeContentProcessing;

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
                    var formatter = new SimpleHTMLFormatter(HighLightPrefix, HighLightSuffix);

                    var highlighter = new Highlighter(formatter, scorer)
                    {
                        TextFragmenter = new SimpleFragmenter(length),
                        MaxDocCharsToAnalyze = maxContentHighlightLength
                    };

                    var stream = analyzer.GetTokenStream(nameof(CodeSource.Content), new StringReader(text));

                    result = highlighter.GetBestFragments(stream, text, 3, "...");
                }

                result = string.IsNullOrEmpty(result) ?
                        (returnRawContentWhenResultIsEmpty ? HttpUtility.HtmlEncode(text) : string.Empty)
                        : HttpUtility.HtmlEncode(result).Replace(HighLightPrefix, prefix).Replace(HighLightSuffix, suffix);
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

        public static (string MatchedLineContent, int LineNumber)[] GeneratePreviewTextWithLineNumber(Query query, string text, int length, Analyzer analyzer, int maxResults, int maxContentHighlightLength = Constants.DefaultMaxContentHighlightLength, bool forWeb = true, bool needReplaceSuffixAndPrefix = true, string prefix = "<label class='highlight'>", string suffix = "</label>")
        {
            string highLightResult = null;
            (string, int)[] results = null;

            if (text.Length <= maxContentHighlightLength) // For performance
            {
                if (query != null)
                {
                    var scorer = new QueryScorer(query);
                    var formatter = new SimpleHTMLFormatter(HighLightPrefix, HighLightSuffix);

                    var highlighter = new Highlighter(formatter, scorer)
                    {
                        TextFragmenter = new SimpleFragmenter(length),
                        MaxDocCharsToAnalyze = maxContentHighlightLength
                    };

                    var stream = analyzer.GetTokenStream(nameof(CodeSource.Content), new StringReader(text));

                    highLightResult = highlighter.GetBestFragments(stream, text, 3, "...");
                }

                highLightResult ??= string.Empty;

                var matchedLines = new List<(string, int)>();
                using var stringReader = new StringReader(highLightResult);
                string line;
                var lineNumber = 0;
                while ((line = stringReader.ReadLine()?.Trim()) != null)
                {
                    lineNumber++;

                    if (matchedLines.Count < maxResults)
                    {
                        if (line.Contains(HighLightPrefix) || line.Contains(HighLightSuffix))
                        {
                            string matchedLineFormatted;

                            if (needReplaceSuffixAndPrefix)
                            {
                                matchedLineFormatted = forWeb ? HttpUtility.HtmlEncode(line).Replace(HighLightPrefix, prefix).Replace(HighLightSuffix, suffix) : line.Replace(HighLightPrefix, prefix).Replace(HighLightSuffix, suffix);
                            }
                            else
                            {
                                matchedLineFormatted = forWeb ? HttpUtility.HtmlEncode(line) : line;
                            }

                            matchedLines.Add((matchedLineFormatted, lineNumber));
                        }
                    }
                    else
                    {
                        break;
                    }
                };

                results = matchedLines.ToArray();
            }
            else
            {
                results = new[]
                {
                    ("Content is too long to highlight", 1)
                };
            }

            return results;
        }
    }
}
