using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using CodeIndex.Common;
using CodeIndex.IndexBuilder;
using CodeIndex.Search;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Search;
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
        }

        readonly IConfiguration config;
        readonly ILog log;

        [HttpGet]
        [Route(nameof(GetCodeSources))]
        public FetchResult<IEnumerable<CodeSource>> GetCodeSources(string searchQuery, bool preview, string contentQuery = "", int? showResults = 0)
        {
            FetchResult<IEnumerable<CodeSource>> result;

            try
            {
                searchQuery.RequireNotNullOrEmpty(nameof(searchQuery));

                var showResultsValue = showResults.HasValue && showResults.Value <= 100 && showResults.Value > 0 ? showResults.Value : 100;

                result = new FetchResult<IEnumerable<CodeSource>>
                {
                    Result = SearchCodeSource(searchQuery, out var query, showResultsValue),
                    Status = new Status
                    {
                        Success = true
                    }
                };

                var queryForContent = string.IsNullOrWhiteSpace(contentQuery) ? null : generator.GetQueryFromStr(contentQuery);

                if (preview)
                {
                    foreach (var item in result.Result)
                    {
                        item.Content = CodeIndexSearcher.GenerateHtmlPreviewText(queryForContent, item.Content, 30, generator.Analyzer);
                    }
                }
                else if (!preview)
                {
                    foreach (var item in result.Result)
                    {
                        item.Content = CodeIndexSearcher.GenerateHtmlPreviewText(queryForContent, item.Content, int.MaxValue, generator.Analyzer, returnRawContentWhenResultIsEmpty: true);
                    }
                }

                log.Debug($"Request: '{searchQuery}' successful");
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

        [HttpGet]
        [Route(nameof(GetHints))]
        public FetchResult<IEnumerable<string>> GetHints(string word)
        {
            FetchResult<IEnumerable<string>> result;
            try
            {
                word.RequireNotNullOrEmpty(nameof(word));

                result = new FetchResult<IEnumerable<string>>
                {
                    Result = CodeIndexSearcher.GetHints(LuceneIndex, word),
                    Status = new Status
                    {
                        Success = true
                    }
                };

                log.Debug($"Get Hints For '{word}' successful");
            }
            catch (Exception ex)
            {
                result = new FetchResult<IEnumerable<string>>
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

        CodeSource[] SearchCodeSource(string searchStr, out Query query, int showResults = 100)
        {
            query = generator.GetQueryFromStr(searchStr);
            return CodeIndexSearcher.SearchCode(config["LuceneIndex"], query, showResults > 100 ? 100 : showResults);
        }

        static readonly QueryGenerator generator = new QueryGenerator();
        string LuceneIndex => config["LuceneIndex"];

        [HttpGet]
        [Route(nameof(GetTokenizeStr))]
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
        [Route(nameof(GetLogs))]
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
                    using var fs = new FileStream(logPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    using var sr = new StreamReader(fs, Encoding.Default);
                    result.Result = await sr.ReadToEndAsync();
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

                log.Error(ex.ToString());
            }

            return result;
        }
    }
}
