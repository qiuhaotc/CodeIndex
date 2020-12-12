using System;
using CodeIndex.Common;

namespace CodeIndex.MaintainIndex
{
    public class IndexMaintainerWrapper : IDisposable
    {
        IndexStatus? statusOverride;

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

        public IndexStatus Status
        {
            get
            {
                return statusOverride ?? Maintainer.Status;
            }
            set
            {
                statusOverride = value;
            }
        }

        public IndexConfig IndexConfig { get; }

        public void Dispose()
        {
            if (!IsDisposing)
            {
                IsDisposing = true;
                Maintainer.Dispose();
            }
        }
    }
}
