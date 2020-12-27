using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using CodeIndex.Common;
using CodeIndex.IndexBuilder;
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Search;
using Lucene.Net.Search.Spans;

namespace CodeIndex.MaintainIndex
{
    public class QueryGenerator
    {
        QueryParser QueryParser { get; }

        public QueryGenerator(QueryParser queryParser)
        {
            queryParser.RequireNotNull(nameof(queryParser));
            QueryParser = queryParser;
        }

        public static string GetSearchStr(string fileName, string content, string fileExtension, string filePath, bool caseSensitive = false, string codePk = null)
        {
            if (!string.IsNullOrWhiteSpace(codePk))
            {
                return $"{nameof(CodeSource.CodePK)}:{codePk}";
            }

            var searchQueries = new List<string>();

            if (!string.IsNullOrWhiteSpace(fileName))
            {
                searchQueries.Add($"{nameof(CodeSource.FileName)}:{fileName}");
            }

            var contentPart = GetSearchStr(content, caseSensitive);
            if (contentPart != null)
            {
                searchQueries.Add(contentPart);
            }

            if (!string.IsNullOrWhiteSpace(fileExtension))
            {
                searchQueries.Add($"{nameof(CodeSource.FileExtension)}:{fileExtension}");
            }

            if (!string.IsNullOrWhiteSpace(filePath))
            {
                if (SurroundWithQuotation(filePath))
                {
                    filePath = filePath.Replace("\\", "\\\\");
                }

                searchQueries.Add($"{nameof(CodeSource.FilePath)}:{filePath}");
            }

            return string.Join(" AND ", searchQueries);
        }

        public static string GetSearchStr(string content, bool caseSensitive)
        {
            if (!string.IsNullOrWhiteSpace(content))
            {
                if (caseSensitive)
                {
                    return $"{CodeIndexBuilder.GetCaseSensitiveField(nameof(CodeSource.Content))}:{content}";
                }

                return $"{nameof(CodeSource.Content)}:{content}";
            }

            return null;
        }

        public Query GetSearchQuery(SearchRequest searchRequest)
        {
            if (!searchRequest.PhaseQuery)
            {
                return GetQueryFromStr(GetSearchStr(searchRequest.FileName, searchRequest.Content, searchRequest.FileExtension, searchRequest.FilePath, searchRequest.CaseSensitive, searchRequest.CodePK));
            }

            if (!string.IsNullOrEmpty(searchRequest.CodePK))
            {
                return GetQueryFromStr($"{nameof(CodeSource.CodePK)}:{searchRequest.CodePK}");
            }

            var query = new BooleanQuery();

            AddPhaseQuery(query, searchRequest.Content, searchRequest.CaseSensitive);
            AddPhaseQuery(query, searchRequest.FileName);
            AddPhaseQuery(query, searchRequest.FileExtension);
            AddPhaseQuery(query, searchRequest.FilePath);

            return query;
        }

        #region Wildcard Phases Query

        const string EncodedDoubleQuotes = "\\\"";
        const string ReplaceEncodedDoubleQuotes = "D17D0790BB624053A86E551E6C0A66F6";

        const string EncodedAsterisk = "\\*";
        const string ReplaceEncodedAsterisk = "7DF41FE6D52A4E62B9175758FE2A5A27";

        const string WildcardAsterisk = "*";
        const string ReplaceWildcardAsterisk = "BC3C0E14C1DA4C1F82C53F6185C662E7";

        void AddPhaseQuery(BooleanQuery query, string queryStr, bool caseSensitive = false, [CallerMemberName] string propertyName = null)
        {
            if (!string.IsNullOrWhiteSpace(queryStr))
            {
                if (caseSensitive && propertyName == nameof(CodeSource.Content))
                {
                    propertyName = CodeIndexBuilder.GetCaseSensitiveField(nameof(CodeSource.Content));
                }

                queryStr = queryStr.Replace(EncodedDoubleQuotes, ReplaceEncodedDoubleQuotes).Replace("\"", string.Empty);

                if (!string.IsNullOrWhiteSpace(queryStr))
                {
                    if (queryStr.Contains(WildcardAsterisk))
                    {
                        queryStr = queryStr.Replace(EncodedAsterisk, ReplaceEncodedAsterisk);

                        var queryFuzzyParts = queryStr.Split(WildcardAsterisk, StringSplitOptions.RemoveEmptyEntries);

                        if (queryFuzzyParts.Length == 0)
                        {
                            AddPhaseQueryWithoutWildcard(query, queryStr.Replace(ReplaceEncodedAsterisk, EncodedAsterisk), propertyName);
                        }
                        else
                        {
                            queryStr = queryStr.Replace(WildcardAsterisk, ReplaceWildcardAsterisk).Replace(ReplaceEncodedDoubleQuotes, "\"").Replace(ReplaceEncodedAsterisk, WildcardAsterisk);

                            var words = new List<string>();

                            using (var tokenStream = LucenePoolLight.Analyzer.GetTokenStream(propertyName, queryStr))
                            {
                                var termAttr = tokenStream.GetAttribute<ICharTermAttribute>();

                                tokenStream.Reset();

                                while (tokenStream.IncrementToken())
                                {
                                    words.Add(termAttr.ToString());
                                }
                            }

                            if (words.Count > 0)
                            {
                                var phraseWords = new List<string>();

                                foreach (var word in words)
                                {
                                    if (word.StartsWith(ReplaceWildcardAsterisk))
                                    {
                                        // TODO: Support searching for "ABC * EDF"
                                        throw new NotImplementedException("Not support wildcard searching at top or wildcard only searching");
                                    }
                                    else
                                    {
                                        phraseWords.Add(word.Replace(WildcardAsterisk, EncodedAsterisk).Replace(ReplaceWildcardAsterisk, WildcardAsterisk));
                                    }
                                }

                                query.Add(CreatePhraseQuery(phraseWords, propertyName), Occur.MUST);
                            }
                        }
                    }
                    else
                    {
                        AddPhaseQueryWithoutWildcard(query, queryStr, propertyName);
                    }
                }
            }
        }

        Query CreatePhraseQuery(List<string> phraseWords, string fieldName)
        {
            var queryParts = new SpanQuery[phraseWords.Count];

            for (int i = 0; i < phraseWords.Count; i++)
            {
                var wildQuery = new WildcardQuery(new Term(fieldName, phraseWords[i]));
                queryParts[i] = new SpanMultiTermQueryWrapper<WildcardQuery>(wildQuery);
            }

            return new SpanNearQuery(
                queryParts, //words
                0, //max distance
                true //exact order
            );
        }

        void AddPhaseQueryWithoutWildcard(BooleanQuery query, string queryStr, string propertyName)
        {
            queryStr = $"\"{queryStr}\"";

            if (propertyName == nameof(CodeSource.FilePath))
            {
                query.Add(GetQueryFromStr($"{propertyName}:{queryStr.Replace("\\", "\\\\").Replace(ReplaceEncodedDoubleQuotes, EncodedDoubleQuotes)}"), Occur.MUST);
            }
            else
            {
                query.Add(GetQueryFromStr($"{propertyName}:{queryStr.Replace(ReplaceEncodedDoubleQuotes, EncodedDoubleQuotes)}"), Occur.MUST);
            }
        }

        #endregion

        public Query GetQueryFromStr(string searchStr)
        {
            searchStr.RequireNotNullOrEmpty(nameof(searchStr));

            return QueryParser.Parse(searchStr);
        }

        static bool SurroundWithQuotation(string content)
        {
            return !string.IsNullOrWhiteSpace(content) && content.StartsWith("\"") && content.EndsWith("\"");
        }
    }
}
