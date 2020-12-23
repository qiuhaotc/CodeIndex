using System.Collections.Generic;
using CodeIndex.Common;
using CodeIndex.IndexBuilder;
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
