using System;
using System.Collections.Generic;
using CodeIndex.IndexBuilder;
using Lucene.Net.Documents;
using NUnit.Framework;

namespace CodeIndex.Test
{
    public class DocumentConverterTest
    {
        [Test]
        public void TestConvert()
        {
            var document = new Document
            {
                new StringField(nameof(DummyForTest.Pk), Guid.NewGuid().ToString(), Field.Store.YES),
                new StringField(nameof(DummyForTest.AAA), "AAA", Field.Store.YES),
                new StringField(nameof(DummyForTest.BBB), "32", Field.Store.YES),
                new StringField(nameof(DummyForTest.CCC), "32.3", Field.Store.YES),
                new StringField(nameof(DummyForTest.DDD), "120.0", Field.Store.YES),
                new Int64Field(nameof(DummyForTest.EEE), DateTime.Now.Ticks, Field.Store.YES),
                new StringField(nameof(DummyForTest.FFF), "A|B|C|D|E", Field.Store.YES),
                new StringField(nameof(DummyForTest.ReadonlyProperty), "ReadonlyProperty", Field.Store.YES),
            };

            var dummyForTest = document.GetObject<DummyForTest>();
            Assert.AreNotEqual(Guid.Empty, dummyForTest.Pk);
            Assert.AreEqual("AAA", dummyForTest.AAA);
            Assert.AreEqual(32, dummyForTest.BBB);
            Assert.AreEqual(32.3, dummyForTest.CCC);
            Assert.AreEqual(120.0f, dummyForTest.DDD);
            Assert.AreNotEqual(new DateTime(), dummyForTest.EEE);
            CollectionAssert.AreEquivalent(new[] { "A", "B", "C", "D", "E" }, dummyForTest.FFF);
            Assert.IsNull(dummyForTest.ReadonlyProperty, "Don't set readonly property");
        }

        [Test]
        public void TestThrowException()
        {
            var document = new Document
            {
                new StringField(nameof(DummyForTest2.BlaBla), "10", Field.Store.YES),
                new StringField(nameof(DummyForTest3.BlaBlaEnum), "32|12", Field.Store.YES),
            };

            Assert.Throws<NotImplementedException>(() => document.GetObject<DummyForTest2>());
            Assert.Throws<NotImplementedException>(() => document.GetObject<DummyForTest3>());
        }

        class DummyForTest
        {
            public Guid Pk { get; set; }
            public string AAA { get; set; }
            public int BBB { get; set; }
            public double CCC { get; set; }
            public float DDD { get; set; }
            public DateTime EEE { get; set; }
            public IEnumerable<string> FFF { get; set; }
            public string ReadonlyProperty { get; }
        }

        class DummyForTest2
        {
            public decimal BlaBla { get; set; }
        }

        class DummyForTest3
        {
            public IEnumerable<decimal> BlaBlaEnum { get; set; }
        }
    }
}
