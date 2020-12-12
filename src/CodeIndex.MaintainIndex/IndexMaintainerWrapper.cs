using System;
using CodeIndex.Common;
using Lucene.Net.QueryParsers.Classic;

namespace CodeIndex.MaintainIndex
{
    public class IndexMaintainerWrapper : IDisposable
    {
        public IndexMaintainerWrapper(IndexConfig indexConfig, CodeIndexConfiguration codeIndexConfiguration, ILog log)
        {
            indexConfig.RequireNotNull(nameof(indexConfig));
            codeIndexConfiguration.RequireNotNull(nameof(codeIndexConfiguration));
            log.RequireNotNull(nameof(log));

            Maintainer = new IndexMaintainer(indexConfig, codeIndexConfiguration, log);
            IndexConfig = indexConfig;
        }

        public IndexMaintainer Maintainer { get; }

        public bool IsDisposing { get; private set; }

        public IndexStatus Status => Maintainer.Status;

        public IndexConfig IndexConfig { get; }

        public void Dispose()
        {
            if (!IsDisposing)
            {
                IsDisposing = true;
                Maintainer.Dispose();
            }
        }

        QueryParser queryParser;
        public QueryParser CodeIndexQueryParser => queryParser ??= Maintainer.IndexBuilderLight.CodeIndexPool.GetQueryParser();

        QueryGenerator queryGenerator;
        public QueryGenerator QueryGenerator => queryGenerator ??= new QueryGenerator(CodeIndexQueryParser);
    }
}
