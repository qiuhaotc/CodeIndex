using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using CodeIndex.VisualStudioExtension.Models;

namespace CodeIndex.VisualStudioExtension
{
    public class CodeIndexSearchViewModel : BaseViewModel
    {
        public CodeIndexSearchViewModel()
        {
            serviceUrl = ConfigHelper.Configuration.AppSettings.Settings[nameof(ServiceUrl)].Value;
            _ = LoadIndexInfosAsync();
        }

        public Guid IndexPk
        {
            get => indexPk;
            set
            {
                indexPk = value;
                NotifyPropertyChange();
            }
        }

        public string FileName { get; set; }
        public string Content { get; set; }
        public string FileExtension { get; set; }
        public string FileLocation { get; set; }
        public int ShowResultsNumber { get; set; } = 1000;

        public string ServiceUrl
        {
            get => serviceUrl;
            set
            {
                if (value != null && value.EndsWith("/"))
                {
                    value = value.Substring(0, value.Length - 1);
                }

                if (value != serviceUrl)
                {
                    ConfigHelper.SetConfiguration(nameof(ServiceUrl), value);
                    serviceUrl = value;
                }

                _ = LoadIndexInfosAsync();
            }
        }

        public bool CaseSensitive { get; set; }

        public bool PhaseQuery { get; set; } = true;

        CancellationTokenSource tokenToLoadIndexInfos;

        async Task LoadIndexInfosAsync()
        {
            try
            {
                tokenToLoadIndexInfos?.Cancel();
                tokenToLoadIndexInfos?.Dispose();
                tokenToLoadIndexInfos = new CancellationTokenSource();

                var client = new CodeIndexClient(new HttpClient(), ServiceUrl);
                var result = await client.ApiLuceneGetindexviewlistAsync(tokenToLoadIndexInfos.Token);

                IndexInfos = result.Status.Success ? result.Result.Select(u => new Item<Guid>(u.IndexName, u.Pk)).ToList() : IndexInfos;

                if (IndexPk == Guid.Empty || IndexInfos.All(u => u.Value != IndexPk))
                {
                    IndexPk = IndexInfos.FirstOrDefault()?.Value ?? Guid.Empty;
                }
                else
                {
                    IndexPk = IndexPk;
                }

                ResultInfo = string.Empty;
            }
            catch (Exception ex)
            {
                ResultInfo = "Exception Occur: " + ex;
            }
        }

        CancellationTokenSource tokenSourceToGetHintWord;

        public async Task GetHintWordsAsync()
        {
            if (string.IsNullOrEmpty(Content))
            {
                return;
            }

            tokenSourceToGetHintWord?.Dispose();
            tokenSourceToGetHintWord = null;

            var inputWord = Content.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
            if (inputWord?.Length >= 3)
            {
                tokenSourceToGetHintWord = new CancellationTokenSource();
                var hintWords = await Task.Run(async () =>
                {
                    try
                    {
                        var client = new CodeIndexClient(new HttpClient(), ServiceUrl);
                        var result = await client.ApiLuceneGethintsAsync(inputWord, IndexPk);

                        if (result.Status.Success)
                        {
                            ResultInfo = string.Empty;
                            return result.Result.Select(u => new HintWord { Word = u }).ToList();
                        }

                        ResultInfo = result.Status.StatusDesc;
                    }
                    catch (Exception ex)
                    {
                        ResultInfo = "Exception Occur: " + ex;
                    }

                    return new List<HintWord>();
                }, tokenSourceToGetHintWord.Token);

                HintWords = hintWords;
                NotifyPropertyChange(nameof(HintWords));
            }
        }

        public string ResultInfo
        {
            get => resultInfo;
            set
            {
                resultInfo = value;
                NotifyPropertyChange();
            }
        }

        public List<HintWord> HintWords { get; set; }

        public Item<int>[] Options { get; } = new[]
        {
            new Item<int>("Top 100", 100),
            new Item<int>("Top 200", 200),
            new Item<int>("Top 500", 500),
            new Item<int>("Top 1000", 1000),
            new Item<int>("Top 10000", 10000)
        };

        public List<CodeSourceWithMatchedLine> SearchResults
        {
            get => searchResults;
            set
            {
                searchResults = value;
                NotifyPropertyChange();
            }
        }

        public List<Item<Guid>> IndexInfos
        {
            get => indexInfos;
            set
            {
                indexInfos = value;
                NotifyPropertyChange();
            }
        }

        public class Item<T>
        {
            public Item(string name, T value)
            {
                Name = name;
                Value = value;
            }

            public string Name { get; }
            public T Value { get; }
        }

        ICommand searchIndexCommand;
        ICommand stopSearchCommand;
        ICommand refreshIndexCommand;
        string serviceUrl;
        List<CodeSourceWithMatchedLine> searchResults = new List<CodeSourceWithMatchedLine>();
        string resultInfo;
        CancellationTokenSource tokenSource;
        List<Item<Guid>> indexInfos = new List<Item<Guid>>();
        Guid indexPk;

        public ICommand SearchIndexCommand
        {
            get
            {
                if (searchIndexCommand == null)
                {
                    searchIndexCommand = new AsyncCommand(
                        SearchCodeIndexAsync,
                        () => !IsSearching,
                        null);
                }
                return searchIndexCommand;
            }
        }

        public ICommand StopSearchCommand
        {
            get
            {
                if (stopSearchCommand == null)
                {
                    stopSearchCommand = new CommonCommand(
                        param => tokenSource?.Cancel(),
                        param => true
                    );
                }
                return stopSearchCommand;
            }
        }

        public ICommand RefreshIndexCommand
        {
            get
            {
                if (refreshIndexCommand == null)
                {
                    refreshIndexCommand = new AsyncCommand(
                        LoadIndexInfosAsync,
                        () => true,
                        null);
                }

                return refreshIndexCommand;
            }
        }

        #region SearchCodeIndex

        bool IsSearching { get; set; }

        async Task SearchCodeIndexAsync()
        {
            if (!IsSearching)
            {
                try
                {
                    IsSearching = true;
                    tokenSource?.Dispose();
                    tokenSource = new CancellationTokenSource();
                    ResultInfo = "Searching...";

                    await SearchCodeIndexCoreAsync();
                }
                catch (TaskCanceledException)
                {
                    ResultInfo = "Search cancelled.";
                }
                catch (Exception ex)
                {
                    ResultInfo = "Exception Occur: " + ex;
                }
                finally
                {
                    IsSearching = false;
                }
            }
        }

        async Task SearchCodeIndexCoreAsync()
        {
            if (IsValidate())
            {
                var client = new CodeIndexClient(new HttpClient(), ServiceUrl);
                var result = await client.ApiLuceneGetcodesourceswithmatchedlineAsync(new SearchRequest
                {
                    IndexPk = indexPk,
                    Content = Content,
                    ShowResults = ShowResultsNumber,
                    NeedReplaceSuffixAndPrefix = false,
                    FileName = FileName,
                    CaseSensitive = CaseSensitive,
                    FileExtension = FileExtension,
                    FilePath = FileLocation,
                    ForWeb = false,
                    Preview = true,
                    PhaseQuery = PhaseQuery
                }, tokenSource.Token);

                if (result.Status.Success)
                {
                    SearchResults = result.Result.ToList();
                }
                else
                {
                    SearchResults.Clear();
                }

                ResultInfo = $"Successful: {result.Status.Success}, Desc: {result.Status.StatusDesc}, Fetch Count: {SearchResults.Count}.";
            }
            else
            {
                ResultInfo = "Search query or index name can't be empty.";
            }
        }

        string GetSearchStr()
        {
            var searchQueries = new List<string>();

            if (!string.IsNullOrWhiteSpace(FileName))
            {
                searchQueries.Add($"{nameof(CodeSource.FileName)}:{FileName}");
            }

            if (!string.IsNullOrWhiteSpace(Content))
            {
                searchQueries.Add($"{nameof(CodeSource.Content)}:{Content}");
            }

            if (!string.IsNullOrWhiteSpace(FileExtension))
            {
                searchQueries.Add($"{nameof(CodeSource.FileExtension)}:{FileExtension}");
            }

            if (!string.IsNullOrWhiteSpace(FileLocation))
            {
                if (SurroundWithQuotation(FileLocation))
                {
                    FileLocation = FileLocation.Replace("\\", "\\\\");
                }

                searchQueries.Add($"{nameof(CodeSource.FilePath)}:{FileLocation}");
            }

            return string.Join(" AND ", searchQueries);
        }

        static bool SurroundWithQuotation(string content)
        {
            return !string.IsNullOrWhiteSpace(content) && content.StartsWith("\"") && content.EndsWith("\"");
        }

        bool IsValidate()
        {
            return !string.IsNullOrEmpty(GetSearchStr());
        }

        #endregion
    }
}
