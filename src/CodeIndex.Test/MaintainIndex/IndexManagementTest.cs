using System;
using System.IO;
using System.Linq;
using CodeIndex.Common;
using CodeIndex.MaintainIndex;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

namespace CodeIndex.Test
{
    public class IndexManagementTest : BaseTest
    {
        [Test]
        public void TestConstructor_InitializeMaintainerPool()
        {
            var indexConfig = new IndexConfig
            {
                IndexName = "ABC",
                MonitorFolder = MonitorFolder
            };

            using var management1 = new IndexManagement(Config, Log);
            CollectionAssert.IsEmpty(management1.GetIndexList().Result);

            management1.AddIndex(indexConfig);

            management1.Dispose();
            using var management2 = new IndexManagement(Config, Log);
            CollectionAssert.AreEquivalent(new[] { indexConfig.ToString() }, management2.GetIndexList().Result.Select(u => u.IndexConfig.ToString()));
        }

        [Test]
        public void TestAddIndex()
        {
            var indexConfig = new IndexConfig
            {
                IndexName = "ABC",
                MonitorFolder = MonitorFolder
            };

            using var management = new IndexManagement(Config, Log);
            CollectionAssert.IsEmpty(management.GetIndexList().Result);

            Assert.IsTrue(management.AddIndex(indexConfig).Status.Success);
            CollectionAssert.AreEquivalent(new[] { indexConfig.ToString() }, management.GetIndexList().Result.Select(u => u.IndexConfig.ToString()));

            Assert.IsFalse(management.AddIndex(indexConfig).Status.Success);
            Assert.IsFalse(management.AddIndex(indexConfig with { IndexName = null }).Status.Success);
            Assert.IsFalse(management.AddIndex(indexConfig with { IndexName = "BBB" }).Status.Success);
            Assert.IsFalse(management.AddIndex(indexConfig with { IndexName = "A".PadLeft(101, 'C') }).Status.Success);
            Assert.IsFalse(management.AddIndex(indexConfig with { Pk = Guid.NewGuid(), IndexName = "BBB", MonitorFolder = string.Empty }).Status.Success);
            Assert.IsFalse(management.AddIndex(indexConfig with { Pk = Guid.NewGuid(), IndexName = "BBB" }).Status.Success);
            Assert.IsFalse(management.AddIndex(indexConfig with { Pk = Guid.NewGuid(), IndexName = "BBB", MonitorFolder = "Dummy" }).Status.Success);
            CollectionAssert.AreEquivalent(new[] { indexConfig.ToString() }, management.GetIndexList().Result.Select(u => u.IndexConfig.ToString()));
        }

        [Test]
        public void TestEditIndex()
        {
            var indexConfig = new IndexConfig
            {
                IndexName = "ABC",
                MonitorFolder = MonitorFolder
            };

            using var management = new IndexManagement(Config, Log);
            CollectionAssert.IsEmpty(management.GetIndexList().Result);

            Assert.IsFalse(management.EditIndex(indexConfig).Status.Success);
            Assert.IsTrue(management.AddIndex(indexConfig).Status.Success);
            CollectionAssert.AreEquivalent(new[] { indexConfig.ToString() }, management.GetIndexList().Result.Select(u => u.IndexConfig.ToString()));

            Assert.IsFalse(management.EditIndex(indexConfig with { IndexName = null }).Status.Success);
            Assert.IsFalse(management.EditIndex(indexConfig with { IndexName = "A".PadLeft(101, 'C') }).Status.Success);
            Assert.IsFalse(management.EditIndex(indexConfig with { Pk = Guid.NewGuid(), IndexName = "BBB", MonitorFolder = string.Empty }).Status.Success);
            Assert.IsFalse(management.EditIndex(indexConfig with { Pk = Guid.NewGuid(), IndexName = "BBB" }).Status.Success);
            Assert.IsFalse(management.EditIndex(indexConfig with { Pk = Guid.NewGuid(), IndexName = "BBB", MonitorFolder = "Dummy" }).Status.Success);
            CollectionAssert.AreEquivalent(new[] { indexConfig.ToString() }, management.GetIndexList().Result.Select(u => u.IndexConfig.ToString()));

            Assert.IsTrue(management.EditIndex(indexConfig with { IndexName = "BBB" }).Status.Success);
            CollectionAssert.AreEquivalent(new[] { (indexConfig with { IndexName = "BBB" }).ToString() }, management.GetIndexList().Result.Select(u => u.IndexConfig.ToString()));
            Assert.IsTrue(management.GetIndexMaintainerWrapperAndInitializeIfNeeded(indexConfig.Pk).Status.Success);
            Assert.IsFalse(management.EditIndex(indexConfig).Status.Success);
        }

        [Test]
        public void TestGetIndexMaintainerWrapperAndInitializeIfNeeded()
        {
            var indexConfig = new IndexConfig
            {
                IndexName = "ABC",
                MonitorFolder = MonitorFolder
            };

            using var management = new IndexManagement(Config, Log);
            CollectionAssert.IsEmpty(management.GetIndexList().Result);

            Assert.IsTrue(management.AddIndex(indexConfig).Status.Success);
            Assert.AreEqual(IndexStatus.Idle, management.GetIndexList().Result.Select(u => u.IndexStatus).First());

            var maintainer = management.GetIndexMaintainerWrapperAndInitializeIfNeeded(indexConfig.Pk);
            Assert.IsTrue(maintainer.Status.Success);
            Assert.AreNotEqual(IndexStatus.Idle, maintainer.Result.Status);
            Assert.IsFalse(management.GetIndexMaintainerWrapperAndInitializeIfNeeded(Guid.NewGuid()).Status.Success);
        }

        [Test]
        public void TestDeleteIndex()
        {
            var indexConfig = new IndexConfig
            {
                IndexName = "ABC",
                MonitorFolder = MonitorFolder
            };

            using var management = new IndexManagement(Config, Log);
            CollectionAssert.IsEmpty(management.GetIndexList().Result);

            Assert.IsTrue(management.AddIndex(indexConfig).Status.Success);
            CollectionAssert.AreEquivalent(new[] { indexConfig.ToString() }, management.GetIndexList().Result.Select(u => u.IndexConfig.ToString()));
            Assert.IsTrue(management.DeleteIndex(indexConfig.Pk).Status.Success);
            Assert.IsFalse(management.DeleteIndex(indexConfig.Pk).Status.Success);
            CollectionAssert.IsEmpty(management.GetIndexList().Result);
        }

        [Test]
        public void TestGetIndexView()
        {
            var indexConfig = new IndexConfig
            {
                IndexName = "ABC",
                MonitorFolder = MonitorFolder
            };

            using var management = new IndexManagement(Config, Log);
            CollectionAssert.IsEmpty(management.GetIndexList().Result);

            Assert.IsTrue(management.AddIndex(indexConfig).Status.Success);
            CollectionAssert.AreEquivalent(new[] { indexConfig.ToString() }, management.GetIndexList().Result.Select(u => u.IndexConfig.ToString()));

            Assert.AreEqual(indexConfig.IndexName, management.GetIndexView(indexConfig.Pk).IndexName);
            Assert.AreEqual(new IndexConfigForView().IndexName, management.GetIndexView(Guid.NewGuid()).IndexName);
        }

        [Test]
        public void TestGetIndexViewList()
        {
            var indexConfig1 = new IndexConfig
            {
                IndexName = "ABC",
                MonitorFolder = MonitorFolder
            };

            Directory.CreateDirectory(Path.Combine(TempDir, "CodeFolder2"));

            var indexConfig2 = new IndexConfig
            {
                IndexName = "EFG",
                MonitorFolder = Path.Combine(TempDir, "CodeFolder2")
            };

            using var management = new IndexManagement(Config, Log);
            CollectionAssert.IsEmpty(management.GetIndexList().Result);

            Assert.IsTrue(management.AddIndex(indexConfig1).Status.Success);
            Assert.IsTrue(management.AddIndex(indexConfig2).Status.Success);

            CollectionAssert.AreEquivalent(new[] { "ABC", "EFG" }, management.GetIndexViewList().Result.Select(u => u.IndexName));

            management.StopIndex(indexConfig1.Pk);
            CollectionAssert.AreEquivalent(new[] { "EFG" }, management.GetIndexViewList().Result.Select(u => u.IndexName));
        }

        [Test]
        public void TestGetIndexList()
        {
            var indexConfig1 = new IndexConfig
            {
                IndexName = "ABC",
                MonitorFolder = MonitorFolder
            };

            Directory.CreateDirectory(Path.Combine(TempDir, "CodeFolder2"));

            var indexConfig2 = new IndexConfig
            {
                IndexName = "EFG",
                MonitorFolder = Path.Combine(TempDir, "CodeFolder2")
            };

            using var management = new IndexManagement(Config, Log);
            CollectionAssert.IsEmpty(management.GetIndexList().Result);

            Assert.IsTrue(management.AddIndex(indexConfig1).Status.Success);
            Assert.IsTrue(management.AddIndex(indexConfig2).Status.Success);

            CollectionAssert.AreEquivalent(new[] { "ABC", "EFG" }, management.GetIndexList().Result.Select(u => u.IndexConfig.IndexName));

            management.DeleteIndex(indexConfig1.Pk);
            CollectionAssert.AreEquivalent(new[] { "EFG" }, management.GetIndexList().Result.Select(u => u.IndexConfig.IndexName));
        }

        [Test]
        public void TestStartIndex()
        {
            var indexConfig = new IndexConfig
            {
                IndexName = "ABC",
                MonitorFolder = MonitorFolder
            };

            using var management = new IndexManagement(Config, Log);
            CollectionAssert.IsEmpty(management.GetIndexList().Result);

            Assert.IsTrue(management.AddIndex(indexConfig).Status.Success);
            Assert.AreEqual(IndexStatus.Idle, management.GetIndexList().Result.Select(u => u.IndexStatus).First());
            Assert.IsTrue(management.StartIndex(indexConfig.Pk).Status.Success);
            Assert.AreNotEqual(IndexStatus.Idle, management.GetIndexList().Result.Select(u => u.IndexStatus).First());
        }

        [Test]
        public void TestStopIndex()
        {
            var indexConfig1 = new IndexConfig
            {
                IndexName = "ABC",
                MonitorFolder = MonitorFolder
            };

            Directory.CreateDirectory(Path.Combine(TempDir, "CodeFolder2"));

            var indexConfig2 = new IndexConfig
            {
                IndexName = "EFG",
                MonitorFolder = Path.Combine(TempDir, "CodeFolder2")
            };

            using var management = new IndexManagement(Config, Log);
            CollectionAssert.IsEmpty(management.GetIndexList().Result);

            Assert.IsTrue(management.AddIndex(indexConfig1).Status.Success);
            Assert.IsTrue(management.AddIndex(indexConfig2).Status.Success);
            CollectionAssert.AreEquivalent(new[] { "ABC", "EFG" }, management.GetIndexViewList().Result.Select(u => u.IndexName));

            Assert.IsTrue(management.StopIndex(indexConfig2.Pk).Status.Success);
            Assert.IsFalse(management.StopIndex(indexConfig2.Pk).Status.Success);
            CollectionAssert.AreEquivalent(new[] { "ABC" }, management.GetIndexViewList().Result.Select(u => u.IndexName));
        }

        protected new ILogger<IndexManagement> Log => log ??= new DummyLog<IndexManagement>();
        ILogger<IndexManagement> log;
    }
}
