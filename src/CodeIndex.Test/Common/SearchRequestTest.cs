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

            Assert.That(request, Is.EqualTo(request with { }));
            Assert.That(request.IsEmpty, Is.False);

            request.IndexPk = Guid.Empty;
            Assert.That(request.IsEmpty, Is.True);

            request.IndexPk = Guid.NewGuid();
            request.Content = string.Empty;
            request.FileExtension = " ";
            request.FilePath = null;
            request.FileName = null;
            Assert.That(request.IsEmpty, Is.False);

            request.CodePK = null;
            Assert.That(request.IsEmpty, Is.True);
        }
    }
}
