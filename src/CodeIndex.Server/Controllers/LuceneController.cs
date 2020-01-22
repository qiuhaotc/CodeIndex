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
            SetReader(config);
        }

        IConfiguration config;

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

        CodeSource[] SearchCodeSource(string searchStr)
        {
	        var query = generator.GetQueryFromStr(searchStr);
	        return CodeIndexSearcher.SearchCode(config["LuceneIndex"], reader, query, 100);
        }

        static QueryGenerator generator = new QueryGenerator();
        static DirectoryReader reader;
        void SetReader(IConfiguration config)
        {
            if (reader == null)
            {
                var directory = FSDirectory.Open(config["LuceneIndex"]);
                reader = DirectoryReader.Open(directory);
            }
        }
    }
}
