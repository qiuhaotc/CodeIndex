using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using CodeIndex.Common;
using CodeIndex.IndexBuilder;
using CodeIndex.MaintainIndex;
using Lucene.Net.Analysis;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Search.Highlight;
using Microsoft.Extensions.Logging;
using static CodeIndex.IndexBuilder.CodeContentProcessing;

namespace CodeIndex.Search
{
    public class CodeIndexSearcher
    {
        public CodeIndexSearcher(IndexManagement indexManagement, ILogger<CodeIndexSearcher> log)
        {
            IndexManagement = indexManagement;
            Log = log;
        }

        public IndexManagement IndexManagement { get; }
        public ILogger Log { get; }

        public CodeSource[] SearchCode(SearchRequest searchRequest)
        {
            searchRequest.RequireNotNull(nameof(searchRequest));
            searchRequest.ShowResults.Value.RequireRange(nameof(searchRequest.ShowResults), int.MaxValue, 1);

            if (searchRequest.IsEmpty)
            {
                return Array.Empty<CodeSource>();
            }

            var maintainer = GetIndexMaintainerWrapper(searchRequest.IndexPk);

            if (maintainer == null || !StatusValid(maintainer))
            {
                return Array.Empty<CodeSource>();
            }

            var query = maintainer.QueryGenerator.GetSearchQuery(searchRequest);
            return maintainer.Maintainer.IndexBuilder.CodeIndexPool.Search(query, searchRequest.ShowResults.Value).Select(CodeIndexBuilder.GetCodeSourceFromDocument).ToArray();
        }

        public string GenerateHtmlPreviewText(SearchRequest searchRequest, string text, int length, string prefix = "<label class='highlight'>", string suffix = "</label>", bool returnRawContentWhenResultIsEmpty = false)
        {
            if (searchRequest == null)
            {
                return returnRawContentWhenResultIsEmpty ? HttpUtility.HtmlEncode(text) : string.Empty;
            }

            var maintainer = GetIndexMaintainerWrapper(searchRequest.IndexPk);

            if (maintainer == null)
            {
                return string.Empty;
            }

            var queryForContent = GetContentQuery(searchRequest, maintainer);

            string result = null;

            var maxContentHighlightLength = maintainer.IndexConfig.MaxContentHighlightLength;

            if (maxContentHighlightLength <= 0)
            {
                maxContentHighlightLength = Constants.DefaultMaxContentHighlightLength;
            }

            if (text.Length <= maxContentHighlightLength) // For performance
            {
                if (queryForContent != null)
                {
                    var scorer = new QueryScorer(queryForContent);
                    var formatter = new SimpleHTMLFormatter(HighLightPrefix, HighLightSuffix);

                    var highlighter = new Highlighter(formatter, scorer)
                    {
                        TextFragmenter = new SimpleFragmenter(length),
                        MaxDocCharsToAnalyze = maxContentHighlightLength
                    };

                    using var stream = GetTokenStream(text, searchRequest.CaseSensitive);

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

        public string[] GetHints(string word, Guid pk, int maxResults = 20, bool caseSensitive = false)
        {
            if (string.IsNullOrWhiteSpace(word))
            {
                return Array.Empty<string>();
            }

            var searchQuery = new BooleanQuery();

            if (caseSensitive)
            {
                var query1 = new TermQuery(new Term(nameof(CodeWord.Word), word));
                var query2 = new PrefixQuery(new Term(nameof(CodeWord.Word), word));
                searchQuery.Add(query1, Occur.SHOULD);
                searchQuery.Add(query2, Occur.SHOULD);
            }
            else
            {
                var query1 = new TermQuery(new Term(nameof(CodeWord.WordLower), word.ToLower()));
                var query2 = new PrefixQuery(new Term(nameof(CodeWord.WordLower), word.ToLower()));
                searchQuery.Add(query1, Occur.SHOULD);
                searchQuery.Add(query2, Occur.SHOULD);
            }

            var maintainer = GetIndexMaintainerWrapper(pk);
            return StatusValid(maintainer) ? maintainer.Maintainer.IndexBuilder.HintIndexPool.Search(searchQuery, maxResults).Select(u => u.Get(nameof(CodeWord.Word))).ToArray() : Array.Empty<string>();
        }

        public Query GetContentQuery(SearchRequest searchRequest)
        {
            return GetContentQuery(searchRequest, GetIndexMaintainerWrapper(searchRequest.IndexPk));
        }

        public Query GetContentQuery(SearchRequest searchRequest, IndexMaintainerWrapper wrapper)
        {
            if (wrapper != null)
            {
                return wrapper.QueryGenerator.GetContentSearchQuery(searchRequest);
            }

            return null;
        }

        public (string MatchedLineContent, int LineNumber)[] GeneratePreviewTextWithLineNumber(Query query, string text, int length, int maxResults, Guid pk, bool forWeb = true, bool needReplaceSuffixAndPrefix = true, string prefix = "<label class='highlight'>", string suffix = "</label>", bool caseSensitive = false)
        {
            (string, int)[] results;

            var maintainer = GetIndexMaintainerWrapper(pk);
            if (maintainer == null)
            {
                return Array.Empty<(string, int)>();
            }

            string highLightResult = null;
            var maxContentHighlightLength = maintainer.IndexConfig.MaxContentHighlightLength;

            if (maxContentHighlightLength <= 0)
            {
                maxContentHighlightLength = Constants.DefaultMaxContentHighlightLength;
            }

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

                    using var stream = GetTokenStream(text, caseSensitive);

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

        TokenStream GetTokenStream(string text, bool caseSensitive)
        {
            return LucenePoolLight.Analyzer.GetTokenStream(caseSensitive ? CodeIndexBuilder.GetCaseSensitiveField(nameof(CodeSource.Content)) : nameof(CodeSource.Content), new StringReader(text));
        }

        IndexMaintainerWrapper GetIndexMaintainerWrapper(Guid pk)
        {
            var result = IndexManagement.GetIndexMaintainerWrapperAndInitializeIfNeeded(pk);
            if (result.Status.Success)
            {
                return result.Result;
            }

            Log.LogError($"Index {pk} not exist in Index Management: {result.Status.StatusDesc}");
            return null;
        }

        bool StatusValid(IndexMaintainerWrapper indexMaintainer) => indexMaintainer != null && (indexMaintainer.Status == IndexStatus.Initialized || indexMaintainer.Status == IndexStatus.Initializing_ComponentInitializeFinished || indexMaintainer.Status == IndexStatus.Monitoring);
    }
}
