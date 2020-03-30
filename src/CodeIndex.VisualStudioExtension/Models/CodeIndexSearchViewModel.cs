using CodeIndex.Common;
using CodeIndex.VisualStudioExtension.Properties;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace CodeIndex.VisualStudioExtension
{
    public class CodeIndexSearchViewModel : BaseViewModel
    {
        public CodeIndexSearchViewModel()
        {
            ServiceUrl = CodeIndexSettings.Default.ServiceUrl;
        }

        public string FileName { get; set; }
        public string Content { get; set; }
        public string FileExtension { get; set; }
        public string FileLocation { get; set; }
        public int ShowResultsNumber { get; set; } = 50;
        public string ServiceUrl
        {
            get => serviceUrl;
            set
            {
                if (value != null && value.EndsWith("/"))
                {
                    value = value.Substring(0, value.Length - 1);
                }

                CodeIndexSettings.Default.ServiceUrl = value;
                serviceUrl = value;
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
            new Item<int>("Top 10", 10),
            new Item<int>("Top 20", 20),
            new Item<int>("Top 50", 50),
            new Item<int>("Top 100", 100),
            new Item<int>("Top 1000", 1000)
        };

        public List<CodeSource> SearchResult
        {
            get => searchResult;
            set
            {
                searchResult = value;
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
        List<CodeSource> searchResult = new List<CodeSource>();
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
                var url = $"{ServiceUrl}/api/lucene/GetCodeSources?searchQuery=" + System.Web.HttpUtility.UrlEncode(GetSearchStr()) + "&showResults=" + ShowResultsNumber + "&preview=true" + "&contentQuery=" + System.Web.HttpUtility.UrlEncode(Content ?? string.Empty);

                var client = new HttpClient();
                var response = await client.GetAsync(url, tokenSource.Token);
                var result = await response.Content.ReadAsAsync<FetchResult<List<CodeSource>>>();

                if (result.Status.Success)
                {
                    SearchResult = result.Result;
                }
                else
                {
                    SearchResult.Clear();
                }

                ResultInfo = $"Successful: {result.Status.Success}, Desc: {result.Status.StatusDesc}, Fetch Count: {SearchResult.Count}.";
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
