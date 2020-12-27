using System;
using CodeIndex.Common;
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
            Assert.IsFalse(request.IsEmpty);

            request.IndexPk = Guid.Empty;
            Assert.IsTrue(request.IsEmpty);

            request.IndexPk = Guid.NewGuid();
            request.Content = string.Empty;
            request.FileExtension = " ";
            request.FilePath = null;
            request.FileName = null;
            Assert.IsFalse(request.IsEmpty);

            request.CodePK = null;
            Assert.IsTrue(request.IsEmpty);
        }
    }
}
