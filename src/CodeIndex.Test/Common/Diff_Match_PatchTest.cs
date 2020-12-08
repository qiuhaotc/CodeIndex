using System;
using CodeIndex.Common;
using NUnit.Framework;

namespace CodeIndex.Test
{
    public class Diff_Match_PatchTest
    {
        [Test]
        public void TestDiff()
        {
            var content1 = "Today is a good day" + Environment.NewLine + "WooHoo";
            var content2 = "Tomorrow is not, what a day" + Environment.NewLine + "WOW";

            var dmp = new Diff_Match_Patch();
            var differents = dmp.Diff_Main(content1, content2);
            dmp.Diff_CleanupSemantic(differents);
            Assert.AreEqual(6, differents.Count);
        }
    }
}
