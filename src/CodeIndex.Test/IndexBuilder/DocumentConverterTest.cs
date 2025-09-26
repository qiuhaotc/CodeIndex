using System;
using System.Collections.Generic;
using CodeIndex.Common;
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
            Assert.That(dummyForTest.Pk, Is.Not.EqualTo(Guid.Empty));
            Assert.That(dummyForTest.AAA, Is.EqualTo("AAA"));
            Assert.That(dummyForTest.BBB, Is.EqualTo(32));
            Assert.That(dummyForTest.CCC, Is.EqualTo(32.3));
            Assert.That(dummyForTest.DDD, Is.EqualTo(120.0f));
            Assert.That(dummyForTest.EEE, Is.Not.EqualTo(new DateTime()));
            Assert.That(dummyForTest.FFF, Is.EquivalentTo(new[] { "A", "B", "C", "D", "E" }));
            Assert.That(dummyForTest.ReadonlyProperty, Is.Null, "Don't set readonly property");

            var config = new IndexConfig
            {
                ExcludedExtensions = "ABC",
                ExcludedPaths = "CDF",
                IncludedExtensions = "QQQ",
                IndexName = "AAA",
                MaxContentHighlightLength = 100,
                MonitorFolder = "BCD",
                MonitorFolderRealPath = "SSS",
                OpenIDEUriFormat = "DDDD",
                SaveIntervalSeconds = 22,
                Pk = Guid.NewGuid()
            };

            document = ConfigIndexBuilder.GetDocument(config);
            Assert.That(document.GetObject<IndexConfig>().ToString(), Is.EqualTo(config.ToString()));
        }

        [Test]
        public void TestThrowException()
        {
            var document = new Document
            {
                new StringField(nameof(DummyForTest2.BlaBla), "10", Field.Store.YES),
                new StringField(nameof(DummyForTest3.BlaBlaEnum), "32|12", Field.Store.YES),
            };

            Assert.That(() => document.GetObject<DummyForTest2>(), Throws.TypeOf<NotImplementedException>());
            Assert.That(() => document.GetObject<DummyForTest3>(), Throws.TypeOf<NotImplementedException>());
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
