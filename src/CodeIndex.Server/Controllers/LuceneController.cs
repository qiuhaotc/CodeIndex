using System;
using System.Collections.Generic;
using CodeIndex.Common;
using CodeIndex.Search;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace CodeIndex.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LuceneController : ControllerBase
    {
        public LuceneController(IConfiguration config)
        {
            this.config = config;
        }

        static QueryGenerator generator = new QueryGenerator();
        IConfiguration config;

        [HttpGet]
        [Route(nameof(GetCodeSources))]
        public FetchResult<IEnumerable<CodeSource>> GetCodeSources(string searchStr)
        {
            FetchResult<IEnumerable<CodeSource>> result;
            try
            {
                var directory = FSDirectory.Open(config["LuncenIndex"]);
                var reader = DirectoryReader.Open(directory);
                var query = generator.GetQueryFromStr(searchStr);
                var codeSources = CodeIndexSearcher.SearchCode(config["LuncenIndex"], reader, query, 100);
                reader.Dispose();

                result = new FetchResult<IEnumerable<CodeSource>>
                {
                    Result = codeSources,
                    Status = new Status
                    {
                        Success = true
                    }
                };
            }
            catch (Exception ex)
            {
                result = new FetchResult<IEnumerable<CodeSource>>
                {
                    Status = new Status
                    {
                        Success = false,
                        StatusDesc = ex.ToString()
                    }
                };
            }

            return result;
        }
    }
}
