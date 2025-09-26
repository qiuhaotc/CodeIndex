using System.Collections.Generic;
using CodeIndex.Common;
using NUnit.Framework;

namespace CodeIndex.Test
{
    public class FetchResultTest
    {
        [Test]
        public void TestConstruct()
        {
            var result = new FetchResult<List<string>>
            {
                Status = new Status
                {
                    StatusDesc = "ABC",
                    Success = true
                },
                Result = new List<string>
                {
                    "1", "2", "3"
                }
            };

            Assert.That(result.Status.StatusDesc, Is.EqualTo("ABC"));
            Assert.That(result.Status.Success, Is.True);
            Assert.That(result.Result, Is.EqualTo(new[] { "1", "2", "3" }));
        }
    }
}
