using System;
using System.Collections.Generic;
using System.Text;
using CodeIndex.Common;
using CodeIndex.IndexBuilder;
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

        readonly IConfiguration config;
        readonly ILog log;

        [HttpGet]
        [Route(nameof(GetCodeSources))]
        public FetchResult<IEnumerable<CodeSource>> GetCodeSources(string searchStr, bool preview = false, string preQuery = "")
        {
            FetchResult<IEnumerable<CodeSource>> result;
            try
            {
                result = new FetchResult<IEnumerable<CodeSource>>
                {
                    Result = SearchCodeSource(searchStr, out var query, preview),
                    Status = new Status
                    {
                        Success = true
                    }
                };

                if (preview)
                {
                    foreach (var item in result.Result)
                    {
                        item.Content = CodeIndexSearcher.GenerateHtmlPreviewText(query, item.Content, 50, generator.Analyzer);
                    }
                }
                else
                {
                    foreach (var item in result.Result)
                    {
                        item.Content = CodeIndexSearcher.GenerateHtmlPreviewText(generator.GetQueryFromStr(preQuery), item.Content, int.MaxValue, generator.Analyzer);
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

        CodeSource[] SearchCodeSource(string searchStr, out Query query, bool needPreprocessing = true)
        {
            query = generator.GetQueryFromStr(searchStr, needPreprocessing);
            return CodeIndexSearcher.SearchCode(config["LuceneIndex"], reader, query, 100);
        }

        static readonly QueryGenerator generator = new QueryGenerator();
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
                Result = GetTokenStr(new StandardAnalyzer(Constants.AppLuceneVersion), searchStr) + Environment.NewLine
                + GetTokenStr(new WhitespaceAnalyzer(Constants.AppLuceneVersion), searchStr) + Environment.NewLine
                + GetTokenStr(new SimpleAnalyzer(Constants.AppLuceneVersion), searchStr) + Environment.NewLine
                + GetTokenStr(new StopAnalyzer(Constants.AppLuceneVersion), searchStr) + Environment.NewLine
                + GetTokenStr(new SimpleCodeAnalyzer(Constants.AppLuceneVersion, true), SimpleCodeContentProcessing.Preprocessing(searchStr), true) + Environment.NewLine
            };
        }

        string GetTokenStr(Analyzer analyzer, string content, bool needRestoreString = false)
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

            return needRestoreString ? SimpleCodeContentProcessing.RestoreString(stringBuilder.ToString()) : stringBuilder.ToString();
        }
    }
}
