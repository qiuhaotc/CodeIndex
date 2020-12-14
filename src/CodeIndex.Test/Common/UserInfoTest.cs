using CodeIndex.Common;
using NUnit.Framework;

namespace CodeIndex.Test
{
    public class UserInfoTest
    {
        [Test]
        public void TestConstructor()
        {
            var userInfo = new UserInfo
            {
                Id = 1,
                Password = "ABC",
                UserName = "EDF"
            };

            Assert.AreEqual(1, userInfo.Id);
            Assert.AreEqual("ABC", userInfo.Password);
            Assert.AreEqual("EDF", userInfo.UserName);
        }
    }
}
