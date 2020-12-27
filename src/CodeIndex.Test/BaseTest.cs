using System;
using System.IO;
using CodeIndex.Common;
using CodeIndex.IndexBuilder;
using CodeIndex.MaintainIndex;
using Lucene.Net.QueryParsers.Classic;
using NUnit.Framework;

namespace CodeIndex.Test
{
    public class BaseTest : BaseTestLight
    {
        public CodeIndexConfiguration Config => new CodeIndexConfiguration
        {
            LuceneIndex = TempIndexDir
        };

        protected string TempConfigDir => Path.Combine(TempIndexDir, CodeIndexConfiguration.ConfigurationIndexFolder);

        [SetUp]
        protected override void SetUp()
        {
            base.SetUp();
        }

        [TearDown]
        protected override void TearDown()
        {
            base.TearDown();
        }
    }
}
