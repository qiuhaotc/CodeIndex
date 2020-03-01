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

            Assert.AreEqual("ABC", result.Status.StatusDesc);
            Assert.AreEqual(true, result.Status.Success);
            CollectionAssert.AreEqual(new[] { "1", "2", "3" }, result.Result);
        }
    }
}
