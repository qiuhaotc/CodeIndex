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
        public static string GetSearchStr(string fileName, string content, string fileExtension, string filePath)
        {
            var searchQueries = new List<string>();

            if (!string.IsNullOrWhiteSpace(fileName))
            {
                searchQueries.Add($"{nameof(CodeSource.FileName)}:{fileName}");
            }

            if (!string.IsNullOrWhiteSpace(content))
            {
                searchQueries.Add($"{nameof(CodeSource.Content)}:{content}");
            }

            if (!string.IsNullOrWhiteSpace(fileExtension))
            {
                searchQueries.Add($"{nameof(CodeSource.FileExtension)}:{fileExtension}");
            }

            if (!string.IsNullOrWhiteSpace(filePath))
            {
                searchQueries.Add($"{nameof(CodeSource.FilePath)}:{filePath}");
            }

            return string.Join(" AND ", searchQueries);
        }

        public Query GetQueryFromStr(string searchStr)
        {
            searchStr.RequireNotNullOrEmpty(nameof(searchStr));

            return parser.Parse(searchStr);
        }

        public Analyzer Analyzer => parser.Analyzer;

        readonly QueryParser parser = LucenePool.GetQueryParser();
    }
}
