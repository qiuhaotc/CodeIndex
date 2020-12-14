using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using CodeIndex.Common;
using CodeIndex.IndexBuilder;
using CodeIndex.MaintainIndex;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Search.Highlight;
using static CodeIndex.IndexBuilder.CodeContentProcessing;

namespace CodeIndex.Search
{
    public class CodeIndexSearcher
    {
        public CodeIndexSearcher(IndexManagement indexManagement, ILog log)
        {
            IndexManagement = indexManagement;
            Log = log;
        }

        public IndexManagement IndexManagement { get; }
        public ILog Log { get; }

        public CodeSource[] SearchCode(string searchStr, out Query query, int maxResults, Guid pk)
        {
            searchStr.RequireNotNullOrEmpty(nameof(searchStr));
            maxResults.RequireRange(nameof(maxResults), int.MaxValue, 1);

            var maintainer = GetIndexMaintainerWrapper(pk);

            if (maintainer == null)
            {
                query = null;
                return Array.Empty<CodeSource>();
            }

            query = maintainer.QueryGenerator.GetQueryFromStr(searchStr);
            return StatusValid(maintainer) ? maintainer.Maintainer.IndexBuilder.CodeIndexPool.Search(query, maxResults).Select(GetCodeSourceFromDocument).ToArray() : Array.Empty<CodeSource>();
        }

        public string GenerateHtmlPreviewText(string contentQuery, string text, int length, Guid pk, string prefix = "<label class='highlight'>", string suffix = "</label>", bool returnRawContentWhenResultIsEmpty = false)
        {
            var maintainer = GetIndexMaintainerWrapper(pk);

            if (maintainer == null)
            {
                return string.Empty;
            }

            var queryForContent = string.IsNullOrWhiteSpace(contentQuery) ? null : maintainer.QueryGenerator.GetQueryFromStr(contentQuery);

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

                    using var stream = LucenePoolLight.Analyzer.GetTokenStream(nameof(CodeSource.Content), new StringReader(text));

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

        public Query GetQueryFromStr(string contentQuery, Guid pk)
        {
            return GetIndexMaintainerWrapper(pk)?.QueryGenerator.GetQueryFromStr(contentQuery);
        }

        public (string MatchedLineContent, int LineNumber)[] GeneratePreviewTextWithLineNumber(Query query, string text, int length, int maxResults, Guid pk, bool forWeb = true, bool needReplaceSuffixAndPrefix = true, string prefix = "<label class='highlight'>", string suffix = "</label>")
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

                    using var stream = LucenePoolLight.Analyzer.GetTokenStream(nameof(CodeSource.Content), new StringReader(text));

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

        public static CodeSource GetCodeSourceFromDocument(Document document)
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

        IndexMaintainerWrapper GetIndexMaintainerWrapper(Guid pk)
        {
            var result = IndexManagement.GetIndexMaintainerWrapperAndInitializeIfNeeded(pk);
            if (result.Status.Success)
            {
                return result.Result;
            }

            Log.Info($"Index {pk} not exist in Index Management: {result.Status.StatusDesc}");
            return null;
        }

        bool StatusValid(IndexMaintainerWrapper indexMaintainer) => indexMaintainer != null && (indexMaintainer.Status == IndexStatus.Initialized || indexMaintainer.Status == IndexStatus.Initializing_ComponentInitializeFinished || indexMaintainer.Status == IndexStatus.Monitoring);
    }
}
