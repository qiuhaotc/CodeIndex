using CodeIndex.Common;
using NUnit.Framework;

namespace CodeIndex.Test
{
    public class StorageTest
    {
        [Test]
        public void TestConstruct()
        {
            var storage = new Storage
            {
                UserName = "ABC"
            };

            Assert.AreEqual("ABC", storage.UserName);
        }

        [Test]
        public void TestGet_SetOrUpdateValue()
        {
            var storage = new Storage();
            Assert.IsNull(storage.GetValue("ABC"));

            storage.SetOrUpdate("ABC", 2);
            Assert.AreEqual(2, storage.GetValue("ABC"));

            storage.SetOrUpdate("ABC", "ABCD");
            Assert.AreEqual("ABCD", storage.GetValue("ABC"));
        }
    }
}
