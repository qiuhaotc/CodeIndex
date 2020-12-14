using System.Collections.Generic;
using CodeIndex.Common;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Search;

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
                if (SurroundWithQuotation(filePath))
                {
                    filePath = filePath.Replace("\\", "\\\\");
                }

                searchQueries.Add($"{nameof(CodeSource.FilePath)}:{filePath}");
            }

            return string.Join(" AND ", searchQueries);
        }

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
