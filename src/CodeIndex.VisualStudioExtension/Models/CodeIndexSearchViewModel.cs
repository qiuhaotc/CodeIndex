using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;
using System.Windows.Input;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;
using CodeIndex.VisualStudioExtension.Models;

namespace CodeIndex.VisualStudioExtension
{
    public class CodeIndexSearchViewModel : BaseViewModel
    {
        public CodeIndexSearchViewModel()
        {
            userSettings = UserSettingsManager.Load();
            serviceUrl = userSettings.ServiceUrl;

            // 使用 VS 提供的 ThreadHelper.JoinableTaskFactory，避免 VSSDK005（不要自建 JoinableTaskContext）
            jtf = ThreadHelper.JoinableTaskFactory;
            trackedTasks = jtf.Context.CreateCollection();
            trackedTasks.Add(jtf.RunAsync(LoadIndexInfosAsync)); // 加入集合以便被视为“已观察”从而避免 VSSDK007
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
                    userSettings.ServiceUrl = value;
                    UserSettingsManager.Save(userSettings);
                    serviceUrl = value;
                }

                // 重新加载索引列表并加入集合
                trackedTasks?.Add(jtf.RunAsync(LoadIndexInfosAsync));
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
    ICommand openSettingsCommand;
    string serviceUrl;
    readonly UserSettings userSettings;
    JoinableTaskCollection trackedTasks; // 跟踪后台任务集合
    JoinableTaskFactory jtf;             // VS 提供的 JoinableTaskFactory
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

        public ICommand OpenSettingsCommand
        {
            get
            {
                if (openSettingsCommand == null)
                {
                    openSettingsCommand = new AsyncCommand(OpenSettingsAsync, () => true, null);
                }
                return openSettingsCommand;
            }
        }

        async Task OpenSettingsAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            try
            {
                var settings = UserSettingsManager.Load(); // Ensure directory
                var path = UserSettingsManager.SettingsFile;
                if (!System.IO.File.Exists(path))
                {
                    UserSettingsManager.Save(settings); // create file
                }

                var dte = Package.GetGlobalService(typeof(EnvDTE.DTE)) as EnvDTE.DTE;
                if (dte == null)
                {
                    System.Windows.MessageBox.Show("Cannot get DTE service.", "CodeIndex", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                    return;
                }

                dte.ItemOperations.OpenFile(path);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Open settings failed: " + ex.Message, "CodeIndex", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
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

        #region HintWords 调度封装
        public void ScheduleGetHintWords()
        {
            if (string.IsNullOrEmpty(Content))
            {
                return;
            }

            // 将获取提示词操作作为后台任务加入集合（内部自行捕获异常）
            trackedTasks?.Add(jtf.RunAsync(async delegate
            {
                try
                {
                    await GetHintWordsAsync();
                }
                catch
                {
                    // 忽略异常，提示词非关键路径
                }
            }));
        }
        #endregion
    }
}
