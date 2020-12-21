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

            using var wrapper1 = new IndexManagement(Config, Log);
            CollectionAssert.IsEmpty(wrapper1.GetIndexList().Result);

            wrapper1.AddIndex(indexConfig);

            wrapper1.Dispose();
            using var wrapper2 = new IndexManagement(Config, Log);
            CollectionAssert.AreEquivalent(new[] { indexConfig.ToString() }, wrapper2.GetIndexList().Result.Select(u => u.IndexConfig.ToString()));
        }

        [Test]
        public void TestAddIndex()
        {
            var indexConfig = new IndexConfig
            {
                IndexName = "ABC",
                MonitorFolder = MonitorFolder
            };

            using var wrapper = new IndexManagement(Config, Log);
            CollectionAssert.IsEmpty(wrapper.GetIndexList().Result);

            Assert.IsTrue(wrapper.AddIndex(indexConfig).Status.Success);
            CollectionAssert.AreEquivalent(new[] { indexConfig.ToString() }, wrapper.GetIndexList().Result.Select(u => u.IndexConfig.ToString()));

            Assert.IsFalse(wrapper.AddIndex(indexConfig).Status.Success);
            Assert.IsFalse(wrapper.AddIndex(indexConfig with { IndexName = null }).Status.Success);
            Assert.IsFalse(wrapper.AddIndex(indexConfig with { IndexName = "BBB" }).Status.Success);
            Assert.IsFalse(wrapper.AddIndex(indexConfig with { IndexName = "A".PadLeft(101, 'C') }).Status.Success);
            Assert.IsFalse(wrapper.AddIndex(indexConfig with { Pk = Guid.NewGuid(), IndexName = "BBB", MonitorFolder = string.Empty }).Status.Success);
            Assert.IsFalse(wrapper.AddIndex(indexConfig with { Pk = Guid.NewGuid(), IndexName = "BBB" }).Status.Success);
            Assert.IsFalse(wrapper.AddIndex(indexConfig with { Pk = Guid.NewGuid(), IndexName = "BBB", MonitorFolder = "Dummy" }).Status.Success);
            CollectionAssert.AreEquivalent(new[] { indexConfig.ToString() }, wrapper.GetIndexList().Result.Select(u => u.IndexConfig.ToString()));
        }

        [Test]
        public void TestEditIndex()
        {
            var indexConfig = new IndexConfig
            {
                IndexName = "ABC",
                MonitorFolder = MonitorFolder
            };

            using var wrapper = new IndexManagement(Config, Log);
            CollectionAssert.IsEmpty(wrapper.GetIndexList().Result);

            Assert.IsFalse(wrapper.EditIndex(indexConfig).Status.Success);
            Assert.IsTrue(wrapper.AddIndex(indexConfig).Status.Success);
            CollectionAssert.AreEquivalent(new[] { indexConfig.ToString() }, wrapper.GetIndexList().Result.Select(u => u.IndexConfig.ToString()));

            Assert.IsFalse(wrapper.EditIndex(indexConfig with { IndexName = null }).Status.Success);
            Assert.IsFalse(wrapper.EditIndex(indexConfig with { IndexName = "A".PadLeft(101, 'C') }).Status.Success);
            Assert.IsFalse(wrapper.EditIndex(indexConfig with { Pk = Guid.NewGuid(), IndexName = "BBB", MonitorFolder = string.Empty }).Status.Success);
            Assert.IsFalse(wrapper.EditIndex(indexConfig with { Pk = Guid.NewGuid(), IndexName = "BBB" }).Status.Success);
            Assert.IsFalse(wrapper.EditIndex(indexConfig with { Pk = Guid.NewGuid(), IndexName = "BBB", MonitorFolder = "Dummy" }).Status.Success);
            CollectionAssert.AreEquivalent(new[] { indexConfig.ToString() }, wrapper.GetIndexList().Result.Select(u => u.IndexConfig.ToString()));

            Assert.IsTrue(wrapper.EditIndex(indexConfig with { IndexName = "BBB" }).Status.Success);
            CollectionAssert.AreEquivalent(new[] { (indexConfig with { IndexName = "BBB" }).ToString() }, wrapper.GetIndexList().Result.Select(u => u.IndexConfig.ToString()));
            Assert.IsTrue(wrapper.GetIndexMaintainerWrapperAndInitializeIfNeeded(indexConfig.Pk).Status.Success);
            Assert.IsFalse(wrapper.EditIndex(indexConfig).Status.Success);
        }

        [Test]
        public void TestGetIndexMaintainerWrapperAndInitializeIfNeeded()
        {
            var indexConfig = new IndexConfig
            {
                IndexName = "ABC",
                MonitorFolder = MonitorFolder
            };

            using var wrapper = new IndexManagement(Config, Log);
            CollectionAssert.IsEmpty(wrapper.GetIndexList().Result);

            Assert.IsTrue(wrapper.AddIndex(indexConfig).Status.Success);
            Assert.AreEqual(IndexStatus.Idle, wrapper.GetIndexList().Result.Select(u => u.IndexStatus).First());

            var maintainer = wrapper.GetIndexMaintainerWrapperAndInitializeIfNeeded(indexConfig.Pk);
            Assert.IsTrue(maintainer.Status.Success);
            Assert.AreNotEqual(IndexStatus.Idle, maintainer.Result.Status);
            Assert.IsFalse(wrapper.GetIndexMaintainerWrapperAndInitializeIfNeeded(Guid.NewGuid()).Status.Success);
        }

        [Test]
        public void TestDeleteIndex()
        {
            var indexConfig = new IndexConfig
            {
                IndexName = "ABC",
                MonitorFolder = MonitorFolder
            };

            using var wrapper = new IndexManagement(Config, Log);
            CollectionAssert.IsEmpty(wrapper.GetIndexList().Result);

            Assert.IsTrue(wrapper.AddIndex(indexConfig).Status.Success);
            CollectionAssert.AreEquivalent(new[] { indexConfig.ToString() }, wrapper.GetIndexList().Result.Select(u => u.IndexConfig.ToString()));
            Assert.IsTrue(wrapper.DeleteIndex(indexConfig.Pk).Status.Success);
            Assert.IsFalse(wrapper.DeleteIndex(indexConfig.Pk).Status.Success);
            CollectionAssert.IsEmpty(wrapper.GetIndexList().Result);
        }

        [Test]
        public void TestGetIndexView()
        {
            var indexConfig = new IndexConfig
            {
                IndexName = "ABC",
                MonitorFolder = MonitorFolder
            };

            using var wrapper = new IndexManagement(Config, Log);
            CollectionAssert.IsEmpty(wrapper.GetIndexList().Result);

            Assert.IsTrue(wrapper.AddIndex(indexConfig).Status.Success);
            CollectionAssert.AreEquivalent(new[] { indexConfig.ToString() }, wrapper.GetIndexList().Result.Select(u => u.IndexConfig.ToString()));

            Assert.AreEqual(indexConfig.IndexName, wrapper.GetIndexView(indexConfig.Pk).IndexName);
            Assert.AreEqual(new IndexConfigForView().IndexName, wrapper.GetIndexView(Guid.NewGuid()).IndexName);
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

            using var wrapper = new IndexManagement(Config, Log);
            CollectionAssert.IsEmpty(wrapper.GetIndexList().Result);

            Assert.IsTrue(wrapper.AddIndex(indexConfig1).Status.Success);
            Assert.IsTrue(wrapper.AddIndex(indexConfig2).Status.Success);

            CollectionAssert.AreEquivalent(new[] { "ABC", "EFG" }, wrapper.GetIndexViewList().Result.Select(u => u.IndexName));

            wrapper.StopIndex(indexConfig1.Pk);
            CollectionAssert.AreEquivalent(new[] { "EFG" }, wrapper.GetIndexViewList().Result.Select(u => u.IndexName));
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

            using var wrapper = new IndexManagement(Config, Log);
            CollectionAssert.IsEmpty(wrapper.GetIndexList().Result);

            Assert.IsTrue(wrapper.AddIndex(indexConfig1).Status.Success);
            Assert.IsTrue(wrapper.AddIndex(indexConfig2).Status.Success);

            CollectionAssert.AreEquivalent(new[] { "ABC", "EFG" }, wrapper.GetIndexList().Result.Select(u => u.IndexConfig.IndexName));

            wrapper.DeleteIndex(indexConfig1.Pk);
            CollectionAssert.AreEquivalent(new[] { "EFG" }, wrapper.GetIndexList().Result.Select(u => u.IndexConfig.IndexName));
        }

        [Test]
        public void TestStartIndex()
        {
            var indexConfig = new IndexConfig
            {
                IndexName = "ABC",
                MonitorFolder = MonitorFolder
            };

            using var wrapper = new IndexManagement(Config, Log);
            CollectionAssert.IsEmpty(wrapper.GetIndexList().Result);

            Assert.IsTrue(wrapper.AddIndex(indexConfig).Status.Success);
            Assert.AreEqual(IndexStatus.Idle, wrapper.GetIndexList().Result.Select(u => u.IndexStatus).First());
            Assert.IsTrue(wrapper.StartIndex(indexConfig.Pk).Status.Success);
            Assert.AreNotEqual(IndexStatus.Idle, wrapper.GetIndexList().Result.Select(u => u.IndexStatus).First());
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

            using var wrapper = new IndexManagement(Config, Log);
            CollectionAssert.IsEmpty(wrapper.GetIndexList().Result);

            Assert.IsTrue(wrapper.AddIndex(indexConfig1).Status.Success);
            Assert.IsTrue(wrapper.AddIndex(indexConfig2).Status.Success);
            CollectionAssert.AreEquivalent(new[] { "ABC", "EFG" }, wrapper.GetIndexViewList().Result.Select(u => u.IndexName));

            Assert.IsTrue(wrapper.StopIndex(indexConfig2.Pk).Status.Success);
            Assert.IsFalse(wrapper.StopIndex(indexConfig2.Pk).Status.Success);
            CollectionAssert.AreEquivalent(new[] { "ABC" }, wrapper.GetIndexViewList().Result.Select(u => u.IndexName));
        }

        protected new ILogger<IndexManagement> Log => log ??= new DummyLog<IndexManagement>();
        ILogger<IndexManagement> log;
    }
}
