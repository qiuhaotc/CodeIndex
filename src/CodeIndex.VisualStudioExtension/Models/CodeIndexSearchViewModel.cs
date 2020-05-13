using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using CodeIndex.Common;

namespace CodeIndex.VisualStudioExtension
{
    public class CodeIndexSearchViewModel : BaseViewModel
    {
        public CodeIndexSearchViewModel()
        {
            serviceUrl = ConfigHelper.Configuration.AppSettings.Settings[nameof(ServiceUrl)].Value;
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

                if(value != serviceUrl)
                {
                    ConfigHelper.SetConfiguration(nameof(ServiceUrl), value);
                    serviceUrl = value;
                }
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
        string serviceUrl;
        List<CodeSourceWithMatchedLine> searchResults = new List<CodeSourceWithMatchedLine>();
        string resultInfo;
        CancellationTokenSource tokenSource;

        public ICommand SearchIndexCommand
        {
            get
            {
                if (searchIndexCommand == null)
                {
                    searchIndexCommand = new CommonCommand(
                        param => SearchCodeIndexAsync(),
                        param => true
                    );
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
                var url = $"{ServiceUrl}/api/lucene/GetCodeSourcesWithMatchedLine?searchQuery=" + System.Web.HttpUtility.UrlEncode(GetSearchStr()) + "&showResults=" + ShowResultsNumber + "&contentQuery=" + System.Web.HttpUtility.UrlEncode(Content ?? string.Empty) + "&needReplaceSuffixAndPrefix=false&forWeb=false";

                var client = new HttpClient();
                var response = await client.GetAsync(url, tokenSource.Token);
                var result = await response.Content.ReadAsAsync<FetchResult<List<CodeSourceWithMatchedLine>>>();

                if (result.Status.Success)
                {
                    SearchResults = result.Result;
                }
                else
                {
                    SearchResults.Clear();
                }

                ResultInfo = $"Successful: {result.Status.Success}, Desc: {result.Status.StatusDesc}, Fetch Count: {SearchResults.Count}.";
            }
            else
            {
                ResultInfo = "Search query can't be empty.";
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

        public class CommonCommand : ICommand
        {
            readonly Action<object> execute;
            readonly Predicate<object> canExecute;

            public CommonCommand(Action<object> execute) : this(execute, null)
            {
            }

            public CommonCommand(Action<object> execute, Predicate<object> canExecute)
            {
                if (execute == null)
                    throw new ArgumentNullException("execute");

                this.execute = execute;
                this.canExecute = canExecute;
            }

            [DebuggerStepThrough]
            public bool CanExecute(object parameters)
            {
                return canExecute == null ? true : canExecute(parameters);
            }

            public event EventHandler CanExecuteChanged
            {
                add { CommandManager.RequerySuggested += value; }
                remove { CommandManager.RequerySuggested -= value; }
            }

            public void Execute(object parameters)
            {
                execute(parameters);
            }
        }
    }
}
