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

            Assert.That(storage.UserName, Is.EqualTo("ABC"));
        }

        [Test]
        public void TestGet_SetOrUpdateValue()
        {
            var storage = new Storage();
            Assert.That(storage.GetValue("ABC"), Is.Null);

            storage.SetOrUpdate("ABC", 2);
            Assert.That(storage.GetValue("ABC"), Is.EqualTo(2));

            storage.SetOrUpdate("ABC", "ABCD");
            Assert.That(storage.GetValue("ABC"), Is.EqualTo("ABCD"));
        }
    }
}
