using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
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
    public class CodeIndexSearcherLight
    {
        public CodeIndexSearcherLight(IndexManagement indexManagement, ILog log)
        {
            IndexManagement = indexManagement;
            Log = log;
        }

        public IndexManagement IndexManagement { get; }
        public ILog Log { get; }

        public CodeSource[] SearchCode(string searchStr, out Query query, int maxResults, string indexName)
        {
            searchStr.RequireNotNullOrEmpty(nameof(searchStr));
            indexName.RequireNotNullOrEmpty(nameof(indexName));
            maxResults.RequireRange(nameof(maxResults), int.MaxValue, 1);

            var maintainer = GetIndexMaintainerWrapper(indexName);

            if (maintainer == null)
            {
                query = null;
                return Array.Empty<CodeSource>();
            }

            query = maintainer.QueryGenerator.GetQueryFromStr(searchStr);
            return StatusValid(maintainer) ? maintainer.Maintainer.IndexBuilderLight.CodeIndexPool.Search(query, maxResults).Select(GetCodeSourceFromDocument).ToArray() : Array.Empty<CodeSource>();
        }

        public string GenerateHtmlPreviewText(string contentQuery, string text, int length, string indexName, string prefix = "<label class='highlight'>", string suffix = "</label>", bool returnRawContentWhenResultIsEmpty = false)
        {
            var maintainer = GetIndexMaintainerWrapper(indexName);

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

        public string[] GetHints(string word, string indexName, int maxResults = 20, bool caseSensitive = false)
        {
            if (string.IsNullOrWhiteSpace(word))
            {
                return Array.Empty<string>();
            }

            PrefixQuery query;

            if (caseSensitive)
            {
                query = new PrefixQuery(new Term(nameof(CodeWord.Word), word));
            }
            else
            {
                query = new PrefixQuery(new Term(nameof(CodeWord.WordLower), word.ToLower()));
            }

            var maintainer = GetIndexMaintainerWrapper(indexName);
            return StatusValid(maintainer) ? maintainer.Maintainer.IndexBuilderLight.HintIndexPool.Search(query, maxResults).Select(u => u.Get(nameof(CodeWord.Word))).ToArray() : Array.Empty<string>();
        }

        public Query GetQueryFromStr(string contentQuery, string indexName)
        {
            return GetIndexMaintainerWrapper(indexName)?.QueryGenerator.GetQueryFromStr(contentQuery);
        }

        public (string MatchedLineContent, int LineNumber)[] GeneratePreviewTextWithLineNumber(Query query, string text, int length, int maxResults, string indexName, bool forWeb = true, bool needReplaceSuffixAndPrefix = true, string prefix = "<label class='highlight'>", string suffix = "</label>")
        {
            (string, int)[] results;

            var maintainer = GetIndexMaintainerWrapper(indexName);
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

        public CodeSource GetCodeSourceFromDocument(Document document)
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

        object syncLock = new object();

        IndexMaintainerWrapper GetIndexMaintainerWrapper(string indexName)
        {
            var result = IndexManagement.GetIndexMaintainerWrapper(indexName);
            if (result.Status.Success)
            {
                // Make sure InitializeIndex and MaintainIndexes only call once, make sure the index pool initialized and able to searching
                if (result.Result.Maintainer.Status == IndexStatus.Idle || result.Result.Maintainer.Status == IndexStatus.Initializing)
                {
                    lock (syncLock)
                    {
                        if (result.Result.Maintainer.Status == IndexStatus.Idle)
                        {
                            Log.Info($"Start initializing and monitoring for index {indexName}");
                            result.Result.Maintainer.InitializeIndex(false).ContinueWith(u => result.Result.Maintainer.MaintainIndexes());
                        }

                        if (result.Result.Maintainer.Status == IndexStatus.Initializing)
                        {
                            while (result.Result.Maintainer.Status == IndexStatus.Initializing) // Wait Maintainer able to screening
                            {
                                Thread.Sleep(100);
                            }
                        }
                    }
                }

                return result.Result;
            }

            Log.Info($"Index {indexName} not exist in Index Management: {result.Status.StatusDesc}");
            return null;
        }

        bool StatusValid(IndexMaintainerWrapper indexMaintainer) => indexMaintainer != null && (indexMaintainer.Status == IndexStatus.Initialized || indexMaintainer.Status == IndexStatus.Initializing_ComponentInitializeFinished || indexMaintainer.Status == IndexStatus.Monitoring);
    }
}
