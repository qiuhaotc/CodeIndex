using System;
using System.Collections.Generic;
using System.Text;
using CodeIndex.Common;
using CodeIndex.Search;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Analysis.TokenAttributes;
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

        [HttpGet]
        [Route(nameof(GetTokenizeStr))]
        public FetchResult<string> GetTokenizeStr(string searchStr)
        {
            return new FetchResult<string>()
            {
                Result = GetTokenStr(new StandardAnalyzer(Lucene.Net.Util.LuceneVersion.LUCENE_48), searchStr) + Environment.NewLine
                + GetTokenStr(new WhitespaceAnalyzer(Lucene.Net.Util.LuceneVersion.LUCENE_48), searchStr) + Environment.NewLine
                + GetTokenStr(new SimpleAnalyzer(Lucene.Net.Util.LuceneVersion.LUCENE_48), searchStr) + Environment.NewLine
                + GetTokenStr(new StopAnalyzer(Lucene.Net.Util.LuceneVersion.LUCENE_48), searchStr) + Environment.NewLine
            };
        }

        string GetTokenStr(Analyzer analyzer, string content)
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine(analyzer.GetType().FullName);

            var tokenStream = analyzer.GetTokenStream("A", content ?? string.Empty);

            var termAttr = tokenStream.GetAttribute<ICharTermAttribute>();

            tokenStream.Reset();

            while (tokenStream.IncrementToken())
            {
                stringBuilder.AppendLine(termAttr.ToString());
            }

            return stringBuilder.ToString();
        }
    }
}
