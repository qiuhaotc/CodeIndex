using System;
using System.Collections.Generic;
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
        QueryParser QueryParserNormal { get; }
        QueryParser QueryParserCaseSensitive { get; }

        public QueryGenerator(QueryParser queryParserNormal, QueryParser queryParserCaseSensitive)
        {
            queryParserNormal.RequireNotNull(nameof(queryParserNormal));
            queryParserCaseSensitive.RequireNotNull(nameof(queryParserCaseSensitive));
            QueryParserNormal = queryParserNormal;
            QueryParserCaseSensitive = queryParserCaseSensitive;
        }

        public Query GetSearchQuery(SearchRequest searchRequest)
        {
            if (!searchRequest.PhaseQuery)
            {
                var searchStr1 = GetSearchStr(searchRequest.FileName, searchRequest.FileExtension, searchRequest.FilePath, searchRequest.CodePK);
                Query query1 = null;
                if (!string.IsNullOrWhiteSpace(searchStr1))
                {
                    query1 = GetQueryFromStr(searchStr1, false);
                }

                var searchStr2 = GetContentSearchStr(searchRequest.Content, searchRequest.CaseSensitive);
                Query query2 = null;
                if (!string.IsNullOrWhiteSpace(searchStr2) && string.IsNullOrWhiteSpace(searchRequest.CodePK))
                {
                    query2 = GetQueryFromStr(searchStr2, searchRequest.CaseSensitive);
                }

                if (query1 != null && query2 != null)
                {
                    var searchQuery = new BooleanQuery();
                    searchQuery.Add(query1, Occur.MUST);
                    searchQuery.Add(query2, Occur.MUST);

                    return searchQuery;
                }

                return query1 ?? query2 ?? throw new ArgumentException("Empty search request");
            }

            if (!string.IsNullOrEmpty(searchRequest.CodePK))
            {
                return GetQueryFromStr($"{nameof(CodeSource.CodePK)}:{searchRequest.CodePK}", false);
            }

            var query = new BooleanQuery();

            AddPhaseQuery(query, searchRequest.Content, searchRequest.CaseSensitive, nameof(CodeSource.Content));
            AddPhaseQuery(query, searchRequest.FileName, searchRequest.CaseSensitive, nameof(CodeSource.FileName));
            AddPhaseQuery(query, searchRequest.FileExtension, searchRequest.CaseSensitive, nameof(CodeSource.FileExtension));
            AddPhaseQuery(query, searchRequest.FilePath, searchRequest.CaseSensitive, nameof(CodeSource.FilePath));

            return query;
        }

        public Query GetContentSearchQuery(SearchRequest searchRequest)
        {
            if (string.IsNullOrWhiteSpace(searchRequest.Content))
            {
                return null;
            }

            if (!searchRequest.PhaseQuery)
            {
                return GetQueryFromStr(GetContentSearchStr(searchRequest.Content, searchRequest.CaseSensitive), searchRequest.CaseSensitive);
            }

            var query = new BooleanQuery();

            AddPhaseQuery(query, searchRequest.Content, searchRequest.CaseSensitive, nameof(CodeSource.Content));

            return query;
        }

        #region Wildcard Phases Query

        // TODO: Add Tests

        const string SpecialPrefix = "d780ba3b";
        const string EncodedSpecialPrefix = SpecialPrefix + "0";

        const string DoubleQuotes = "\"";
        const string EncodedDoubleQuotes = "\\\"";
        const string ReplaceEncodedDoubleQuotes = SpecialPrefix + "1";

        const string EncodedAsterisk = "\\*";
        const string ReplaceEncodedAsterisk = SpecialPrefix + "2";

        const string WildcardAsterisk = "*";
        const string ReplaceWildcardAsterisk = SpecialPrefix + "3";

        void AddPhaseQuery(BooleanQuery query, string queryStr, bool caseSensitive, string propertyName)
        {
            if (!string.IsNullOrWhiteSpace(queryStr))
            {
                queryStr = queryStr.Replace(SpecialPrefix, EncodedSpecialPrefix);

                if (caseSensitive && propertyName == nameof(CodeSource.Content))
                {
                    propertyName = CodeIndexBuilder.GetCaseSensitiveField(nameof(CodeSource.Content));
                }

                queryStr = queryStr.Replace(EncodedDoubleQuotes, ReplaceEncodedDoubleQuotes).Replace(DoubleQuotes, string.Empty);

                if (!string.IsNullOrWhiteSpace(queryStr))
                {
                    if (queryStr.Contains(WildcardAsterisk))
                    {
                        queryStr = queryStr.Replace(EncodedAsterisk, ReplaceEncodedAsterisk);

                        if (!queryStr.Contains(WildcardAsterisk))
                        {
                            AddPhaseQueryWithoutWildcard(query, queryStr.Replace(ReplaceEncodedAsterisk, EncodedAsterisk), propertyName);
                        }
                        else
                        {
                            queryStr = queryStr.Replace(WildcardAsterisk, ReplaceWildcardAsterisk).Replace(ReplaceEncodedDoubleQuotes, DoubleQuotes).Replace(ReplaceEncodedAsterisk, WildcardAsterisk);

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
                                        phraseWords.Add(word.Replace(WildcardAsterisk, EncodedAsterisk).Replace(ReplaceWildcardAsterisk, WildcardAsterisk).Replace(EncodedSpecialPrefix, SpecialPrefix));
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
                query.Add(GetQueryFromStr($"{propertyName}:{queryStr.Replace("\\", "\\\\").Replace(ReplaceEncodedDoubleQuotes, EncodedDoubleQuotes).Replace(EncodedSpecialPrefix, SpecialPrefix)}", false), Occur.MUST);
            }
            else
            {
                if (propertyName == CodeIndexBuilder.GetCaseSensitiveField(nameof(CodeSource.Content)))
                {
                    query.Add(GetQueryFromStr($"{propertyName}:{queryStr.Replace(ReplaceEncodedDoubleQuotes, EncodedDoubleQuotes).Replace(EncodedSpecialPrefix, SpecialPrefix)}", true), Occur.MUST);
                }
                else
                {
                    query.Add(GetQueryFromStr($"{propertyName}:{queryStr.Replace(ReplaceEncodedDoubleQuotes, EncodedDoubleQuotes).Replace(EncodedSpecialPrefix, SpecialPrefix)}", false), Occur.MUST);
                }
            }
        }

        #endregion

        public Query GetQueryFromStr(string searchStr, bool caseSensitive)
        {
            searchStr.RequireNotNullOrEmpty(nameof(searchStr));

            return caseSensitive ? QueryParserCaseSensitive.Parse(searchStr) : QueryParserNormal.Parse(searchStr);
        }

        bool SurroundWithQuotation(string content)
        {
            return !string.IsNullOrWhiteSpace(content) && content.StartsWith("\"") && content.EndsWith("\"");
        }

        protected string GetSearchStr(string fileName, string fileExtension, string filePath, string codePk = null)
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

        protected string GetContentSearchStr(string content, bool caseSensitive)
        {
            if (!string.IsNullOrWhiteSpace(content))
            {
                if (caseSensitive)
                {
                    return $"{CodeIndexBuilder.GetCaseSensitiveField(nameof(CodeSource.Content))}:{content}";
                }

                return $"{nameof(CodeSource.Content)}:{content}";
            }

            return string.Empty;
        }
    }
}
