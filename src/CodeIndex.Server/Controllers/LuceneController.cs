using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using CodeIndex.Common;
using CodeIndex.IndexBuilder;
using CodeIndex.MaintainIndex;
using CodeIndex.Search;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Analysis.TokenAttributes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace CodeIndex.Server.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class LuceneController : ControllerBase
    {
        SearchService SearchService { get; }
        ILogger<LuceneController> Log { get; }

        public LuceneController(SearchService searchService, ILogger<LuceneController> log)
        {
            SearchService = searchService;
            Log = log;
        }

        [HttpPost]
        public FetchResult<IEnumerable<CodeSource>> GetCodeSources(SearchRequest searchRequest)
        {
            return SearchService.GetCodeSources(searchRequest);
        }

        [HttpPost]
        public FetchResult<IEnumerable<CodeSourceWithMatchedLine>> GetCodeSourcesWithMatchedLine(SearchRequest searchRequest)
        {
            return SearchService.GetCodeSourcesWithMatchedLine(searchRequest);
        }

        [HttpGet]
        public FetchResult<IEnumerable<string>> GetHints(string word, Guid indexPk)
        {
            return SearchService.GetHints(word, indexPk);
        }

        [HttpGet]
        public FetchResult<string> GetTokenizeStr(string searchStr)
        {
            return new FetchResult<string>()
            {
                Result = GetTokenStr(new StandardAnalyzer(Constants.AppLuceneVersion), searchStr) + Environment.NewLine
                + GetTokenStr(new WhitespaceAnalyzer(Constants.AppLuceneVersion), searchStr) + Environment.NewLine
                + GetTokenStr(new SimpleAnalyzer(Constants.AppLuceneVersion), searchStr) + Environment.NewLine
                + GetTokenStr(new StopAnalyzer(Constants.AppLuceneVersion), searchStr) + Environment.NewLine
                + GetTokenStr(new CodeAnalyzer(Constants.AppLuceneVersion, true), searchStr) + Environment.NewLine
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

        [HttpGet]
        public async Task<FetchResult<string>> GetLogs()
        {
            FetchResult<string> result;
            try
            {
                var logPath = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "Logs", "CodeIndex.log");

                result = new FetchResult<string>
                {
                    Status = new Status
                    {
                        Success = true
                    }
                };

                if (System.IO.File.Exists(logPath))
                {
                    await using var fs = new FileStream(logPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    using var sr = new StreamReader(fs, Encoding.Default);
                    result.Result = await sr.ReadToEndAsync();
                    result.Result = result.Result.SubStringSafe(result.Result.Length - 100000, 100000);
                }
                else
                {
                    result.Result = $"Log Not Exist In {logPath}";
                }
            }
            catch (Exception ex)
            {
                result = new FetchResult<string>
                {
                    Status = new Status
                    {
                        Success = false,
                        StatusDesc = ex.ToString()
                    }
                };

                Log.LogError(ex.ToString());
            }

            return result;
        }

        [HttpGet]
        public async Task<FetchResult<IndexConfigForView[]>> GetIndexViewList([FromServices] IndexManagement indexManagement)
        {
            return await Task.FromResult(indexManagement.GetIndexViewList());
        }
    }
}
