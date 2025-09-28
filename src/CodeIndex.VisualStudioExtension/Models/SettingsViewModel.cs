using System;
using System.ComponentModel;
using System.IO;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Win32;
using System.Diagnostics;
using System.Linq; // For FirstOrDefault
using System.IO.Compression; // For Zip extraction
using System.Collections.Generic; // For Queue in log tail

namespace CodeIndex.VisualStudioExtension.Models
{
    public class SettingsViewModel : INotifyPropertyChanged
    {
        // 简单扩展帮助方法
        // 放在类内部避免额外文件；.NET Framework 无 StartProcess 扩展
        Process StartProcess(ProcessStartInfo psi)
        {
            return Process.Start(psi);
        }
        readonly UserSettings settings;
        bool isBusy;
        double downloadProgress; // 0-100
        string healthStatus = "Unknown"; // Started / Stopped / Error / Unknown
        bool isCheckingHealth;
        bool isOperatingServer; // 启动/停止/重启中的状态，避免按钮重复点击

        public SettingsViewModel(UserSettings settings)
        {
            this.settings = settings;
            RemoteServiceUrl = settings.RemoteServiceUrl;
            LocalServiceUrl = settings.LocalServiceUrl;
            LocalServerInstallPath = settings.LocalServerInstallPath;
            LocalServerDataDirectory = settings.LocalServerDataDirectory;
            LocalServerVersion = settings.LocalServerVersion;
            IsLocalMode = settings.Mode == ServerMode.Local;
            IsRemoteMode = settings.Mode == ServerMode.Remote;

            // 打开设置界面立即触发一次健康检查（异步，不阻塞 UI）
            if (IsLocalMode && !string.IsNullOrWhiteSpace(LocalServiceUrl))
            {
                _ = CheckHealthAsync();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        void Raise([CallerMemberName] string p = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));

        public bool IsRemoteMode
        {
            get => !IsLocalMode;
            set
            {
                if (value)
                {
                    IsLocalMode = false;
                    Raise();
                    Raise(nameof(IsLocalMode));
                }
            }
        }

        bool isLocalMode;
        public bool IsLocalMode
        {
            get => isLocalMode;
            set
            {
                if (isLocalMode != value)
                {
                    isLocalMode = value;
                    Raise();
                    Raise(nameof(IsRemoteMode));
                }
            }
        }

        public string RemoteServiceUrl { get; set; }
        public string LocalServiceUrl { get; set; }
        public string LocalServerInstallPath { get; set; }
        public string LocalServerDataDirectory { get; set; }
        public string LocalServerVersion { get; set; }

        public double DownloadProgress
        {
            get => downloadProgress;
            set { downloadProgress = value; Raise(); }
        }

        public string HealthStatus
        {
            get => healthStatus;
            set { healthStatus = value; Raise(); Raise(nameof(IsServerRunning)); RefreshButtonsState(); }
        }

        public bool IsServerRunning => string.Equals(HealthStatus, "Started", StringComparison.OrdinalIgnoreCase);

        void RefreshButtonsState()
        {
            System.Windows.Input.CommandManager.InvalidateRequerySuggested();
            Raise(nameof(CanStart));
            Raise(nameof(CanStop));
            Raise(nameof(CanRestart));
            startServerCommand?.RaiseCanExecuteChanged();
            stopServerCommand?.RaiseCanExecuteChanged();
            restartServerCommand?.RaiseCanExecuteChanged();
        }

        public bool CanStart => !isBusy && !isOperatingServer && IsLocalMode && !IsServerRunning;
        public bool CanStop => !isBusy && !isOperatingServer && IsLocalMode && IsServerRunning;
        public bool CanRestart => !isBusy && !isOperatingServer && IsLocalMode && IsServerRunning;

        public ICommand BrowseInstallPathCommand => new CommonCommand(_ =>
        {
            var dlg = new System.Windows.Forms.FolderBrowserDialog();
            if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                LocalServerInstallPath = dlg.SelectedPath;
                Raise(nameof(LocalServerInstallPath));
            }
        }, _ => true);

        public ICommand BrowseDataDirCommand => new CommonCommand(_ =>
        {
            var dlg = new System.Windows.Forms.FolderBrowserDialog();
            if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                LocalServerDataDirectory = dlg.SelectedPath;
                Raise(nameof(LocalServerDataDirectory));
            }
        }, _ => true);

        public ICommand DownloadOrUpdateCommand => downloadCommand ?? (downloadCommand = new AsyncCommand(DownloadOrUpdateAsync, () => !isBusy, null));
        public ICommand SaveCommand => new CommonCommand(Save, _ => !isBusy);
        public ICommand StartServerCommand => startServerCommand ?? (startServerCommand = new AsyncCommand(StartServerAsync, () => CanStart, null));
        public ICommand StopServerCommand => stopServerCommand ?? (stopServerCommand = new AsyncCommand(StopServerAsync, () => CanStop, null));
        public ICommand RestartServerCommand => restartServerCommand ?? (restartServerCommand = new AsyncCommand(RestartServerAsync, () => CanRestart, null));
        public ICommand RefreshLogCommand => new AsyncCommand(RefreshLogAsync, () => true, null);
        public ICommand CheckHealthCommand => new AsyncCommand(async () => await CheckHealthAsync(), () => !isCheckingHealth, null);

        AsyncCommand downloadCommand;
        AsyncCommand startServerCommand;
        AsyncCommand stopServerCommand;
        AsyncCommand restartServerCommand;

        public string LogContent
        {
            get => logContent;
            set { logContent = value; Raise(); }
        }
        string logContent;

        // 不再在 ViewModel 中直接保存进程；统一由 LocalServerLauncher 负责生命周期

        const string ReleaseListUrl = "https://github.com/qiuhaotc/CodeIndex/releases"; // 用于解析最新 tag
        const string FixedZipUrlTemplate = "https://github.com/qiuhaotc/CodeIndex/releases/download/{0}/CodeIndex.Server.zip"; // {tag}
        static readonly HttpClient sharedHttp = new HttpClient();

        async Task DownloadOrUpdateAsync()
        {
            try
            {
                isBusy = true; Raise(nameof(IsLocalMode)); // 触发命令刷新

                if (string.IsNullOrWhiteSpace(LocalServerInstallPath))
                {
                    LocalServerInstallPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CodeIndex.VisualStudioExtension", "CodeIndex.Server");
                    Raise(nameof(LocalServerInstallPath));
                }
                Directory.CreateDirectory(LocalServerInstallPath);

                // 解析 releases 页面简单提取第一个 href="/qiuhaotc/CodeIndex/releases/tag/v..."
                string html = await sharedHttp.GetStringAsync(ReleaseListUrl);
                var tag = ParseFirstTag(html) ?? "v0.98_t"; // 回退已知版本
                if (tag.StartsWith("/qiuhaotc/CodeIndex/releases/tag/"))
                {
                    tag = tag.Substring("/qiuhaotc/CodeIndex/releases/tag/".Length);
                }

                // 修正: 之前只比较版本号, 若本地版本号已保存但实际文件缺失会误判为无需下载。
                // 判定需要下载的条件:
                // 1. 未记录版本(LocalServerVersion 为空)
                // 2. 版本不一致
                // 3. 关键文件( CodeIndex.Server.dll ) 不存在 (可能被用户手动删除或首次尚未真正下载)
                var serverDllPath = string.IsNullOrWhiteSpace(LocalServerInstallPath)
                    ? null
                    : Path.Combine(LocalServerInstallPath, "CodeIndex.Server.dll");
                bool serverInstalled = !string.IsNullOrWhiteSpace(serverDllPath) && File.Exists(serverDllPath);
                bool needDownload = string.IsNullOrWhiteSpace(LocalServerVersion)
                                    || !string.Equals(LocalServerVersion, tag, StringComparison.OrdinalIgnoreCase)
                                    || !serverInstalled;
                if (!needDownload)
                {
                    System.Windows.MessageBox.Show($"Already latest: {tag}", "CodeIndex", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                    return;
                }

                var zipUrl = string.Format(FixedZipUrlTemplate, tag);
                var tempZip = Path.Combine(Path.GetTempPath(), $"CodeIndex.Server_{tag}.zip");
                try
                {
                    DownloadProgress = 0;
                    using (var resp = await sharedHttp.GetAsync(zipUrl, HttpCompletionOption.ResponseHeadersRead))
                    {
                        resp.EnsureSuccessStatusCode();
                        var total = resp.Content.Headers.ContentLength;
                        using (var rs = await resp.Content.ReadAsStreamAsync())
                        using (var fs = new FileStream(tempZip, FileMode.Create, FileAccess.Write, FileShare.None))
                        {
                            var buffer = new byte[81920];
                            long readTotal = 0;
                            int read;
                            while ((read = await rs.ReadAsync(buffer, 0, buffer.Length)) > 0)
                            {
                                await fs.WriteAsync(buffer, 0, read);
                                readTotal += read;
                                if (total.HasValue && total.Value > 0)
                                {
                                    DownloadProgress = Math.Round(readTotal * 100.0 / total.Value, 1);
                                }
                            }
                            DownloadProgress = 100;
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show("Download failed: " + ex.Message, "CodeIndex", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    return;
                }

                // 解压：简单处理，若存在旧文件尝试覆盖
                try
                {
                    // 解压前先尝试停止当前运行的服务器
                    await StopServerCoreAsync();
                    // 自定义覆盖解压，兼容 .NET Framework (无 ExtractToDirectory(..., overwriteFiles))
                    using (var archive = ZipFile.OpenRead(tempZip))
                    {
                        foreach (var entry in archive.Entries)
                        {
                            var destinationPath = Path.Combine(LocalServerInstallPath, entry.FullName);
                            if (string.IsNullOrEmpty(entry.Name))
                            {
                                Directory.CreateDirectory(destinationPath);
                                continue;
                            }
                            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));
                            if (File.Exists(destinationPath))
                            {
                                try { File.Delete(destinationPath); } catch { }
                            }
                            entry.ExtractToFile(destinationPath);
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show("Extract failed: " + ex.Message, "CodeIndex", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    return;
                }

                LocalServerVersion = tag;
                Raise(nameof(LocalServerVersion));
                System.Windows.MessageBox.Show($"Updated to {tag}", "CodeIndex", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
            finally
            {
                isBusy = false;
            }
        }

        static string ParseFirstTag(string html)
        {
            if (string.IsNullOrEmpty(html)) return null;
            // 粗略查找 pattern：/qiuhaotc/CodeIndex/releases/tag/v...
            var marker = "/qiuhaotc/CodeIndex/releases/tag/";
            var idx = html.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (idx < 0) return null;
            var start = idx;
            var end = html.IndexOf('"', start + marker.Length);
            if (end < 0) return null;
            return html.Substring(start, end - start);
        }

        async Task StartServerCoreAsync() => await CodeIndex.VisualStudioExtension.Models.LocalServerLauncher.EnsureServerRunningAsync(settings, System.Threading.CancellationToken.None);
        async Task StopServerCoreAsync() { await CodeIndex.VisualStudioExtension.Models.LocalServerLauncher.StopServerIfLastAsync(settings); }

        async Task StartServerAsync()
        {
            if (isOperatingServer) return;
            try
            {
                isOperatingServer = true; RefreshButtonsState();
                HealthStatus = "Starting"; // 立即反馈
                await StartServerCoreAsync();
                await PollHealthAsync(maxAttempts: 12, delayMs: 500, successStatus: "Started", failStatus: "Error");
            }
            finally
            {
                isOperatingServer = false; RefreshButtonsState();
            }
        }

        async Task StopServerAsync()
        {
            if (isOperatingServer) return;
            try
            {
                isOperatingServer = true; RefreshButtonsState();
                HealthStatus = "Stopping";
                await StopServerCoreAsync();
                // 简单等待一小段然后检查
                await Task.Delay(300);
                await PollHealthAsync(maxAttempts: 5, delayMs: 400, successStatus: "Stopped", failStatus: "Stopped", expectStopped: true);
            }
            finally
            {
                isOperatingServer = false; RefreshButtonsState();
            }
        }

        async Task RestartServerAsync()
        {
            if (isOperatingServer) return;
            try
            {
                isOperatingServer = true; RefreshButtonsState();
                HealthStatus = "Restarting";
                await StopServerCoreAsync();
                await Task.Delay(400); // 给进程退出一点时间
                await StartServerCoreAsync();
                await PollHealthAsync(maxAttempts: 14, delayMs: 500, successStatus: "Started", failStatus: "Error");
            }
            finally
            {
                isOperatingServer = false; RefreshButtonsState();
            }
        }

        async Task PollHealthAsync(int maxAttempts, int delayMs, string successStatus, string failStatus, bool expectStopped = false)
        {
            for (int i = 0; i < maxAttempts; i++)
            {
                await Task.Delay(delayMs);
                try
                {
                    var url = LocalServiceUrl?.TrimEnd('/') + "/api/Lucene/GetIndexViewList";
                    if (string.IsNullOrWhiteSpace(LocalServiceUrl)) break;
                    using (var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(2)))
                    {
                        var resp = await sharedHttp.GetAsync(url, cts.Token);
                        if (expectStopped)
                        {
                            if (!resp.IsSuccessStatusCode)
                            {
                                HealthStatus = successStatus; return;
                            }
                        }
                        else if (resp.IsSuccessStatusCode)
                        {
                            HealthStatus = successStatus; return;
                        }
                    }
                }
                catch
                {
                    if (expectStopped)
                    {
                        HealthStatus = successStatus; return;
                    }
                }
            }
            // 超时未达到期望
            if (expectStopped)
            {
                HealthStatus = successStatus; // 停止判定上宽松
            }
            else
            {
                // 若已经是 Started 则不覆盖
                if (!IsServerRunning)
                    HealthStatus = failStatus;
            }
        }

        async Task RefreshLogAsync()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(LocalServerInstallPath)) return;
                var logFile = Path.Combine(LocalServerInstallPath, "Logs", "CodeIndex.log");
                if (!File.Exists(logFile))
                {
                    LogContent = "(log file not found)";
                    return;
                }
                // 读取最新 100 行
                using (var fs = new FileStream(logFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var sr = new StreamReader(fs))
                {
                    var q = new Queue<string>(100);
                    string line;
                    while ((line = await sr.ReadLineAsync()) != null)
                    {
                        if (q.Count == 100) q.Dequeue();
                        q.Enqueue(line);
                    }
                    LogContent = string.Join(Environment.NewLine, q.ToArray());
                }
            }
            catch (Exception ex)
            {
                LogContent = "Read log failed: " + ex.Message;
            }
        }

        void Save(object _)
        {
            settings.RemoteServiceUrl = RemoteServiceUrl?.TrimEnd('/');
            settings.LocalServiceUrl = LocalServiceUrl?.TrimEnd('/');
            settings.LocalServerInstallPath = LocalServerInstallPath;
            settings.LocalServerDataDirectory = LocalServerDataDirectory;
            settings.LocalServerVersion = LocalServerVersion;
            settings.Mode = IsLocalMode ? ServerMode.Local : ServerMode.Remote;
            UserSettingsManager.Save(settings);
            CloseWindow(true);
        }

        public async Task CheckHealthAsync()
        {
            if (isCheckingHealth) return;
            if (!IsLocalMode)
            {
                HealthStatus = "Unknown";
                return;
            }
            if (string.IsNullOrWhiteSpace(LocalServiceUrl))
            {
                HealthStatus = "Unknown";
                return;
            }
            try
            {
                isCheckingHealth = true;
                var url = LocalServiceUrl.TrimEnd('/') + "/api/Lucene/GetIndexViewList";
                using (var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(3)))
                {
                    var resp = await sharedHttp.GetAsync(url, cts.Token);
                    if (resp.IsSuccessStatusCode)
                    {
                        HealthStatus = "Started";
                    }
                    else
                    {
                        HealthStatus = "Error";
                    }
                }
            }
            catch
            {
                HealthStatus = "Stopped";
            }
            finally
            {
                isCheckingHealth = false;
            }
        }

        void CloseWindow(bool dialogResult)
        {
            // 通过打开时的 Window 关联关闭
            foreach (System.Windows.Window w in System.Windows.Application.Current.Windows)
            {
                if (w.DataContext == this)
                {
                    w.DialogResult = dialogResult;
                    w.Close();
                    break;
                }
            }
        }
    }
}
