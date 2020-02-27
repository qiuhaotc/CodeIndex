using System;
using System.Collections.Generic;
using CodeIndex.Common;
using CodeIndex.IndexBuilder;
using Lucene.Net.Analysis;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Search;

namespace CodeIndex.Search
{
    public class QueryGenerator
    {
        public Query GetQuery(SearchCandidate[] searchCandidates)
        {
            searchCandidates.RequireContainsElement(nameof(searchCandidates));

            // search with a phrase
            var query = new BooleanQuery();
            foreach (var item in searchCandidates)
            {
                var condition = item.IsAndCondition ? Occur.MUST : Occur.MUST_NOT;

                switch (item.SearchType)
                {
                    case SearchType.Content:
                        query.Add(new TermQuery(new Term(nameof(CodeSource.Content), item.SearchText)), Occur.MUST);
                        break;

                    case SearchType.FileExtension:
                        query.Add(new TermQuery(new Term(nameof(CodeSource.FileExtension), item.SearchText)), Occur.MUST);
                        break;

                    case SearchType.FileName:
                        query.Add(new TermQuery(new Term(nameof(CodeSource.FileName), item.SearchText)), Occur.MUST);
                        break;

                    case SearchType.FilePath:
                        query.Add(new TermQuery(new Term(nameof(CodeSource.FilePath), item.SearchText)), Occur.MUST);
                        break;

                    default:
                        throw new ArgumentException(nameof(item.SearchType));
                }
            }

            return query;
        }

        public static string GetSearchStr(string fileName, string content, string fileExtension, string filePath)
        {
            var searchQueries = new List<string>();

            if (!string.IsNullOrEmpty(fileName))
            {
                searchQueries.Add($"{nameof(CodeSource.FileName)}:{fileName}");
            }

            if (!string.IsNullOrEmpty(content))
            {
                var restoreWhenFinished = false;

                if (content.StartsWith("\"") && content.EndsWith("\""))
                {
                    restoreWhenFinished = true;
                    content = content.SubStringSafe(1, content.Length - 2);
                }

                content = content.Replace(FuzzySymbol, FuzzySymbolReplaceTo);
                content = SimpleCodeContentProcessing.Preprocessing(content).Replace(FuzzySymbolReplaceTo, FuzzySymbol);

                if (restoreWhenFinished)
                {
                    content = $"\"{content}\"";
                }

                searchQueries.Add($"{nameof(CodeSource.Content)}:{content}");
            }

            if (!string.IsNullOrEmpty(fileExtension))
            {
                searchQueries.Add($"{nameof(CodeSource.FileExtension)}:{fileExtension}");
            }

            if (!string.IsNullOrEmpty(filePath))
            {
                searchQueries.Add($"{nameof(CodeSource.FilePath)}:{filePath}");
            }

            return string.Join(" AND ", searchQueries);
        }

        const string FuzzySymbol = "*";
        const string FuzzySymbolReplaceTo = "FORFUZZY";

        public Query GetQueryFromStr(string searchStr)
        {
            searchStr.RequireNotNullOrEmpty(nameof(searchStr));

            return parser.Parse(searchStr);
        }

        public Analyzer Analyzer => parser.Analyzer;

        readonly QueryParser parser = LucenePool.GetQueryParser();
    }
}
