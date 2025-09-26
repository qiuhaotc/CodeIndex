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

            Assert.That(userInfo.Id, Is.EqualTo(1));
            Assert.That(userInfo.Password, Is.EqualTo("ABC"));
            Assert.That(userInfo.UserName, Is.EqualTo("EDF"));
        }
    }
}
