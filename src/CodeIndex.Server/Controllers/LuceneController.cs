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
        public LuceneController(IConfiguration config, ILog log)
        {
            this.config = config;
            this.log = log;
            SetReader(config);
        }

        IConfiguration config;
        ILog log;

        [HttpGet]
        [Route(nameof(GetCodeSources))]
        public FetchResult<IEnumerable<CodeSource>> GetCodeSources(string searchStr)
        {
            FetchResult<IEnumerable<CodeSource>> result;
            try
            {

                result = new FetchResult<IEnumerable<CodeSource>>
                {
                    Result = SearchCodeSource(searchStr),
                    Status = new Status
                    {
                        Success = true
                    }
                };

                log.Debug($"Request: '{searchStr}' sucessful");
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

                log.Error(ex.ToString());
            }

            return result;
        }

        CodeSource[] SearchCodeSource(string searchStr)
        {
	        var query = generator.GetQueryFromStr(searchStr);
	        return CodeIndexSearcher.SearchCode(config["LuceneIndex"], reader, query, 100);
        }

        static QueryGenerator generator = new QueryGenerator();
        static DirectoryReader reader;
        void SetReader(IConfiguration config)
        {
            try
            {

                if (reader == null)
                {
                    var directory = FSDirectory.Open(config["LuceneIndex"]);
                    reader = DirectoryReader.Open(directory);
                }
            }
            catch (Exception ex)
            {
                log.Error(ex.ToString());
                throw;
            }
        }
    }
}
