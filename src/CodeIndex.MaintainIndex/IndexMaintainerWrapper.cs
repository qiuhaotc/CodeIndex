using System;
using CodeIndex.Common;
using CodeIndex.IndexBuilder;
using Lucene.Net.QueryParsers.Classic;
using Microsoft.Extensions.Logging;

namespace CodeIndex.MaintainIndex
{
    public class IndexMaintainerWrapper : IDisposable
    {
        public IndexMaintainerWrapper(IndexConfig indexConfig, CodeIndexConfiguration codeIndexConfiguration, ILogger log)
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

        QueryParser queryParserNormal;
        public QueryParser QueryParserNormal => queryParserNormal ??= LucenePoolLight.GetQueryParser();

        QueryParser queryParserCaseSensitive;
        public QueryParser QueryParserCaseSensitive => queryParserCaseSensitive ??= LucenePoolLight.GetQueryParser(false);

        QueryGenerator queryGenerator;
        public QueryGenerator QueryGenerator => queryGenerator ??= new QueryGenerator(QueryParserNormal, QueryParserCaseSensitive);
    }
}
