using CodeIndex.IndexBuilder;
using NUnit.Framework;

namespace CodeIndex.Test
{
    public class SimpleCodeContentProcessingTest
    {
        [Test]
        public void TestPreprocessing()
        {
            var content = "TestPreprocessing()";
            var preProcessed = SimpleCodeContentProcessing.Preprocessing(content);
            Assert.AreEqual("TestPreprocessing x7zmgdy7kcd(dktyc2bzsa  x7zmgdy7kcd)dktyc2bzsa ", preProcessed);
        }

        [Test]
        public void TestRestoreString()
        {
            var content = "TestPreprocessing x7zmgdy7kcd(dktyc2bzsa  x7zmgdy7kcd)dktyc2bzsa ";
            var restored = SimpleCodeContentProcessing.RestoreString(content);
            Assert.AreEqual("TestPreprocessing()", restored);

            content = $"TestPreprocessing {SimpleCodeContentProcessing.HighLightPrefix}x7zmgdy7kcd(dktyc2bzsa{SimpleCodeContentProcessing.HighLightSuffix}  x7zmgdy7kcd)dktyc2bzsa ";
             restored = SimpleCodeContentProcessing.RestoreString(content);
            Assert.AreEqual($"TestPreprocessing{SimpleCodeContentProcessing.HighLightPrefix}({SimpleCodeContentProcessing.HighLightSuffix})", restored);
        }
    }
}
