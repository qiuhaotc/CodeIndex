using System;
using System.Collections.Generic;
using CodeIndex.Common;
using CodeIndex.Search;
using Lucene.Net.Index;
using Lucene.Net.Search;
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
        public FetchResult<IEnumerable<CodeSource>> GetCodeSources(string searchStr, bool preview = false, string preQuery = "")
        {
            FetchResult<IEnumerable<CodeSource>> result;
            try
            {
                result = new FetchResult<IEnumerable<CodeSource>>
                {
                    Result = SearchCodeSource(searchStr, out var query),
                    Status = new Status
                    {
                        Success = true
                    }
                };

                if (preview)
                {
                    foreach(var item in result.Result)
                    {
                        item.Content = CodeIndexSearcher.GeneratePreviewText(query, item.Content, 50, generator.Analyzer);
                    }
                }
                else
                {
                    foreach (var item in result.Result)
                    {
                        item.Content = CodeIndexSearcher.GeneratePreviewText(generator.GetQueryFromStr(preQuery), item.Content, int.MaxValue, generator.Analyzer);
                    }
                }

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

        CodeSource[] SearchCodeSource(string searchStr, out Query query)
        {
	        query = generator.GetQueryFromStr(searchStr);
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
