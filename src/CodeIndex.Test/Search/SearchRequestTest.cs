using System;
using CodeIndex.Search;
using NUnit.Framework;

namespace CodeIndex.Test
{
    public class SearchRequestTest
    {
        [Test]
        public void TestConstructor()
        {
            var request = new SearchRequest
            {
                CaseSensitive = true,
                CodePK = "ABC",
                Content = "DDD",
                FileExtension = "CS",
                FileName = "CC",
                FilePath = "DD",
                ForWeb = true,
                IndexPk = Guid.NewGuid(),
                NeedReplaceSuffixAndPrefix = true,
                PhaseQuery = true,
                Preview = true,
                ShowResults = 10
            };

            Assert.AreEqual(request, request with { });
        }
    }
}
