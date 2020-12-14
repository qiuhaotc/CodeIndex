using System;
using System.Collections.Generic;
using System.Linq;
using CodeIndex.Common;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;

namespace CodeIndex.IndexBuilder
{
    public class ConfigIndexBuilder : IDisposable
    {
        public ConfigIndexBuilder(string configIndex)
        {
            configIndex.RequireNotNullOrEmpty(nameof(configIndex));

            ConfigPool = new LucenePoolLight(configIndex);
        }

        public IEnumerable<IndexConfig> GetConfigs()
        {
            return ConfigPool.Search(new MatchAllDocsQuery(), int.MaxValue).Select(u => u.GetObject<IndexConfig>());
        }

        public void AddIndexConfig(IndexConfig indexConfig)
        {
            ConfigPool.BuildIndex(new[] { GetDocumet(indexConfig) }, true);
        }

        public void DeleteIndexConfig(Guid pk)
        {
            ConfigPool.DeleteIndex(new Term(nameof(IndexConfig.Pk), pk.ToString()));
            ConfigPool.Commit();
        }

        public void EditIndexConfig(IndexConfig indexConfig)
        {
            ConfigPool.UpdateIndex(new Term(nameof(IndexConfig.Pk), indexConfig.Pk.ToString()), GetDocumet(indexConfig));
            ConfigPool.Commit();
        }

        public bool IsDisposing { get; private set; }
        public LucenePoolLight ConfigPool { get; }

        public void Dispose()
        {
            if (!IsDisposing)
            {
                IsDisposing = true;
                ConfigPool.Dispose();
            }
        }

        public static Document GetDocumet(IndexConfig indexConfig)
        {
            return new Document
            {
                new StringField(nameof(IndexConfig.Pk), indexConfig.Pk.ToString(), Field.Store.YES),
                new StringField(nameof(IndexConfig.IndexName), indexConfig.IndexName.ToStringSafe(), Field.Store.YES),
                new StringField(nameof(IndexConfig.MonitorFolder), indexConfig.MonitorFolder.ToStringSafe(), Field.Store.YES),
                new Int32Field(nameof(IndexConfig.MaxContentHighlightLength), indexConfig.MaxContentHighlightLength, Field.Store.YES),
                new Int32Field(nameof(IndexConfig.SaveIntervalSeconds), indexConfig.SaveIntervalSeconds, Field.Store.YES),
                new StringField(nameof(IndexConfig.OpenIDEUriFormat), indexConfig.OpenIDEUriFormat.ToStringSafe(), Field.Store.YES),
                new StringField(nameof(IndexConfig.MonitorFolderRealPath), indexConfig.MonitorFolderRealPath.ToStringSafe(), Field.Store.YES),
                new StringField(nameof(IndexConfig.ExcludedPaths), indexConfig.ExcludedPaths.ToStringSafe(), Field.Store.YES),
                new StringField(nameof(IndexConfig.IncludedExtensions), indexConfig.IncludedExtensions.ToStringSafe(), Field.Store.YES),
                new StringField(nameof(IndexConfig.ExcludedExtensions), indexConfig.ExcludedExtensions.ToStringSafe(), Field.Store.YES),
            };
        }
    }
}
