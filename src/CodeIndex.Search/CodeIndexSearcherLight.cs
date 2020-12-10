using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using CodeIndex.Common;
using CodeIndex.MaintainIndex;
using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Search.Highlight;
using static CodeIndex.IndexBuilder.CodeContentProcessing;

namespace CodeIndex.Search
{
    public class CodeIndexSearcherLight
    {
        public CodeIndexSearcherLight(IndexMaintainer indexMaintainer)
        {
            IndexMaintainer = indexMaintainer;
        }

        public IndexMaintainer IndexMaintainer { get; }

        public CodeSource[] SearchCode(Query query, int maxResults)
        {
            query.RequireNotNull(nameof(query));
            maxResults.RequireRange(nameof(maxResults), int.MaxValue, 1);

            return StatusValid ? IndexMaintainer.IndexBuilderLight.CodeIndexPool.Search(query, maxResults).Select(GetCodeSourceFromDocument).ToArray() : Array.Empty<CodeSource>();
        }

        public string GenerateHtmlPreviewText(Query query, string text, int length, string prefix = "<label class='highlight'>", string suffix = "</label>", bool returnRawContentWhenResultIsEmpty = false, int maxContentHighlightLength = Constants.DefaultMaxContentHighlightLength)
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

                    var stream = IndexMaintainer.IndexBuilderLight.CodeIndexPool.Analyzer.GetTokenStream(nameof(CodeSource.Content), new StringReader(text));

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

        public string[] GetHints(string word, int maxResults = 20, bool caseSensitive = false)
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

            return StatusValid ? IndexMaintainer.IndexBuilderLight.HintIndexPool.Search(query, maxResults).Select(u => u.Get(nameof(CodeWord.Word))).ToArray() : Array.Empty<string>();
        }

        public (string MatchedLineContent, int LineNumber)[] GeneratePreviewTextWithLineNumber(Query query, string text, int length, Analyzer analyzer, int maxResults, bool forWeb = true, bool needReplaceSuffixAndPrefix = true, string prefix = "<label class='highlight'>", string suffix = "</label>")
        {
            string highLightResult = null;
            (string, int)[] results;

            var maxContentHighlightLength = IndexMaintainer.IndexConfig.MaxContentHighlightLength;

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

        bool StatusValid => IndexMaintainer.Status == IndexStatus.Initialized || IndexMaintainer.Status == IndexStatus.Initializing || IndexMaintainer.Status == IndexStatus.Monitoring;
    }
}
