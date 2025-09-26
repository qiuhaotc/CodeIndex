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
            Assert.That(management1.GetIndexList().Result, Is.Empty);

            management1.AddIndex(indexConfig);

            management1.Dispose();
            using var management2 = new IndexManagement(Config, Log);
            Assert.That(management2.GetIndexList().Result.Select(u => u.IndexConfig.ToString()), Is.EquivalentTo(new[] { indexConfig.ToString() }));
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
            Assert.That(management.GetIndexList().Result, Is.Empty);

            Assert.That(management.AddIndex(indexConfig).Status.Success, Is.True);
            Assert.That(management.GetIndexList().Result.Select(u => u.IndexConfig.ToString()), Is.EquivalentTo(new[] { indexConfig.ToString() }));

            Assert.That(management.AddIndex(indexConfig).Status.Success, Is.False);
            Assert.That(management.AddIndex(indexConfig with { IndexName = null }).Status.Success, Is.False);
            Assert.That(management.AddIndex(indexConfig with { IndexName = "BBB" }).Status.Success, Is.False);
            Assert.That(management.AddIndex(indexConfig with { IndexName = "A".PadLeft(101, 'C') }).Status.Success, Is.False);
            Assert.That(management.AddIndex(indexConfig with { Pk = Guid.NewGuid(), IndexName = "BBB", MonitorFolder = string.Empty }).Status.Success, Is.False);
            Assert.That(management.AddIndex(indexConfig with { Pk = Guid.NewGuid(), IndexName = "BBB" }).Status.Success, Is.False);
            Assert.That(management.AddIndex(indexConfig with { Pk = Guid.NewGuid(), IndexName = "BBB", MonitorFolder = "Dummy" }).Status.Success, Is.False);
            Assert.That(management.GetIndexList().Result.Select(u => u.IndexConfig.ToString()), Is.EquivalentTo(new[] { indexConfig.ToString() }));
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
            Assert.That(management.GetIndexList().Result, Is.Empty);

            Assert.That(management.EditIndex(indexConfig).Status.Success, Is.False);
            Assert.That(management.AddIndex(indexConfig).Status.Success, Is.True);
            Assert.That(management.GetIndexList().Result.Select(u => u.IndexConfig.ToString()), Is.EquivalentTo(new[] { indexConfig.ToString() }));

            Assert.That(management.EditIndex(indexConfig with { IndexName = null }).Status.Success, Is.False);
            Assert.That(management.EditIndex(indexConfig with { IndexName = "A".PadLeft(101, 'C') }).Status.Success, Is.False);
            Assert.That(management.EditIndex(indexConfig with { Pk = Guid.NewGuid(), IndexName = "BBB", MonitorFolder = string.Empty }).Status.Success, Is.False);
            Assert.That(management.EditIndex(indexConfig with { Pk = Guid.NewGuid(), IndexName = "BBB" }).Status.Success, Is.False);
            Assert.That(management.EditIndex(indexConfig with { Pk = Guid.NewGuid(), IndexName = "BBB", MonitorFolder = "Dummy" }).Status.Success, Is.False);
            Assert.That(management.GetIndexList().Result.Select(u => u.IndexConfig.ToString()), Is.EquivalentTo(new[] { indexConfig.ToString() }));

            Assert.That(management.EditIndex(indexConfig with { IndexName = "BBB" }).Status.Success, Is.True);
            Assert.That(management.GetIndexList().Result.Select(u => u.IndexConfig.ToString()), Is.EquivalentTo(new[] { (indexConfig with { IndexName = "BBB" }).ToString() }));
            Assert.That(management.GetIndexMaintainerWrapperAndInitializeIfNeeded(indexConfig.Pk).Status.Success, Is.True);
            Assert.That(management.EditIndex(indexConfig).Status.Success, Is.False);
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
            Assert.That(management.GetIndexList().Result, Is.Empty);

            Assert.That(management.AddIndex(indexConfig).Status.Success, Is.True);
            Assert.That(management.GetIndexList().Result.Select(u => u.IndexStatus).First(), Is.EqualTo(IndexStatus.Idle));

            var maintainer = management.GetIndexMaintainerWrapperAndInitializeIfNeeded(indexConfig.Pk);
            Assert.That(maintainer.Status.Success, Is.True);
            Assert.That(maintainer.Result.Status, Is.Not.EqualTo(IndexStatus.Idle));
            Assert.That(management.GetIndexMaintainerWrapperAndInitializeIfNeeded(Guid.NewGuid()).Status.Success, Is.False);
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
            Assert.That(management.GetIndexList().Result, Is.Empty);

            Assert.That(management.AddIndex(indexConfig).Status.Success, Is.True);
            Assert.That(management.GetIndexList().Result.Select(u => u.IndexConfig.ToString()), Is.EquivalentTo(new[] { indexConfig.ToString() }));
            Assert.That(management.DeleteIndex(indexConfig.Pk).Status.Success, Is.True);
            Assert.That(management.DeleteIndex(indexConfig.Pk).Status.Success, Is.False);
            Assert.That(management.GetIndexList().Result, Is.Empty);
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
            Assert.That(management.GetIndexList().Result, Is.Empty);

            Assert.That(management.AddIndex(indexConfig).Status.Success, Is.True);
            Assert.That(management.GetIndexList().Result.Select(u => u.IndexConfig.ToString()), Is.EquivalentTo(new[] { indexConfig.ToString() }));

            Assert.That(management.GetIndexView(indexConfig.Pk).IndexName, Is.EqualTo(indexConfig.IndexName));
            Assert.That(management.GetIndexView(Guid.NewGuid()).IndexName, Is.EqualTo(new IndexConfigForView().IndexName));
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
            Assert.That(management.GetIndexList().Result, Is.Empty);

            Assert.That(management.AddIndex(indexConfig1).Status.Success, Is.True);
            Assert.That(management.AddIndex(indexConfig2).Status.Success, Is.True);

            Assert.That(management.GetIndexViewList().Result.Select(u => u.IndexName), Is.EquivalentTo(new[] { "ABC", "EFG" }));

            management.StopIndex(indexConfig1.Pk);
            Assert.That(management.GetIndexViewList().Result.Select(u => u.IndexName), Is.EquivalentTo(new[] { "EFG" }));
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
            Assert.That(management.GetIndexList().Result, Is.Empty);

            Assert.That(management.AddIndex(indexConfig1).Status.Success, Is.True);
            Assert.That(management.AddIndex(indexConfig2).Status.Success, Is.True);

            Assert.That(management.GetIndexList().Result.Select(u => u.IndexConfig.IndexName), Is.EquivalentTo(new[] { "ABC", "EFG" }));

            management.DeleteIndex(indexConfig1.Pk);
            Assert.That(management.GetIndexList().Result.Select(u => u.IndexConfig.IndexName), Is.EquivalentTo(new[] { "EFG" }));
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
            Assert.That(management.GetIndexList().Result, Is.Empty);

            Assert.That(management.AddIndex(indexConfig).Status.Success, Is.True);
            Assert.That(management.GetIndexList().Result.Select(u => u.IndexStatus).First(), Is.EqualTo(IndexStatus.Idle));
            Assert.That(management.StartIndex(indexConfig.Pk).Status.Success, Is.True);
            Assert.That(management.GetIndexList().Result.Select(u => u.IndexStatus).First(), Is.Not.EqualTo(IndexStatus.Idle));
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
            Assert.That(management.GetIndexList().Result, Is.Empty);

            Assert.That(management.AddIndex(indexConfig1).Status.Success, Is.True);
            Assert.That(management.AddIndex(indexConfig2).Status.Success, Is.True);
            Assert.That(management.GetIndexViewList().Result.Select(u => u.IndexName), Is.EquivalentTo(new[] { "ABC", "EFG" }));

            Assert.That(management.StopIndex(indexConfig2.Pk).Status.Success, Is.True);
            Assert.That(management.StopIndex(indexConfig2.Pk).Status.Success, Is.False);
            Assert.That(management.GetIndexViewList().Result.Select(u => u.IndexName), Is.EquivalentTo(new[] { "ABC" }));
        }

        protected new ILogger<IndexManagement> Log => log ??= new DummyLog<IndexManagement>();
        ILogger<IndexManagement> log;
    }
}
