using System.Linq;
using CodeIndex.Common;
using CodeIndex.IndexBuilder;
using Lucene.Net.Search;

namespace CodeIndex.Test
{
    public static class SearcherForTest
    {
        public static CodeSource[] SearchCode(this ILucenePool lucenePool, Query searchQuery, int maxResults = int.MaxValue)
        {
            return lucenePool.Search(searchQuery, maxResults).Select(CodeIndexBuilder.GetCodeSourceFromDocument).ToArray();
        }

        public static CodeWord[] SearchWord(this ILucenePool lucenePool, Query searchQuery, int maxResults = int.MaxValue)
        {
            return lucenePool.Search(searchQuery, maxResults).Select(u => new CodeWord
            {
                Word = u.Get(nameof(CodeWord.Word)),
                WordLower = u.Get(nameof(CodeWord.WordLower)),
            }).ToArray();
        }
    }
}
