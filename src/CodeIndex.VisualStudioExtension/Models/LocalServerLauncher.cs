using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Text;

namespace CodeIndex.VisualStudioExtension.Models
{
    internal static class LocalServerLauncher
    {
        static Mutex singleInstanceMutex; // 仅负责“我是否启动过”
        const string MutexName = "Global_CodeIndex_Server_Singleton"; // 跨 VS 实例共享，表示“某 VS 已经负责启动”

        static int localRefCount; // 当前进程（VS 实例）内引用数（窗口等）
        static readonly HttpClient http = new HttpClient();

        // 新的多进程引用保持：为每个 VS 进程创建单独锁文件 (clients/<pid>-<guid>.lock) 持续保持打开
        // 关闭 / 崩溃：进程退出 OS 会释放句柄，后续清理逻辑会剔除僵尸文件（对应 pid 不存在）
        const string ClientLocksFolderName = "clients";
        const string GlobalClientsMutexName = "Global_CodeIndex_Server_ClientLocks"; // 跨进程协调清理/判断
        static string clientLockFilePath;
        static FileStream clientLockStream; // 保持打开以标识本 VS 进程存活

        public static async Task RegisterInstanceAsync(UserSettings settings)
        {
            if (settings == null) return;
            if (settings.Mode != ServerMode.Local) return;
            try
            {
                var after = Interlocked.Increment(ref localRefCount);
                if (after == 1)
                {
                    EnsureClientLockFile(settings);
                }

                // 后台确保运行
                await EnsureServerRunningAsync(settings, CancellationToken.None);
            }
            catch { }
        }

        public static void UnregisterInstance(UserSettings settings)
        {
            if (settings == null) return;
            if (settings.Mode != ServerMode.Local) return;
            try
            {
                var after = Interlocked.Decrement(ref localRefCount);
                if (after < 0) localRefCount = 0;
                if (after == 0)
                {
                    // 尝试如果已经无其它 VS 客户端则停止
                    _ = StopServerIfLastAsync(settings);
                }
            }
            catch { }
        }

        /// <summary>
        /// 确保本地 Server 已运行（通过健康检查判断），若未运行则使用互斥启动。
        /// </summary>
        // 单实例内的去重 / 缓存：防止同一个 VS 进程里高频重复调用导致并发启动尝试
        static readonly object ensureSync = new object();
        static Task<bool> inFlightEnsureTask;
        static DateTime lastHealthyUtc;

        /// <summary>
        /// 去重的“确保服务器运行”，同一时间只有一个真实启动逻辑在执行。
        /// 成功后 10 秒内的重复调用直接返回 true（快速路径）。
        /// </summary>
        public static Task<bool> EnsureServerRunningAsync(UserSettings settings, CancellationToken token)
        {
            if (settings == null || settings.Mode != ServerMode.Local)
            {
                return Task.FromResult(false);
            }

            // 若最近一次健康时间在窗口内，直接返回
            if (lastHealthyUtc != default && (DateTime.UtcNow - lastHealthyUtc) < TimeSpan.FromSeconds(10))
            {
                return Task.FromResult(true);
            }

            // 先做一次快速健康检查，避免不必要锁竞争
            return EnsureSingleFlightAsync(settings, token);
        }

        static Task<bool> EnsureSingleFlightAsync(UserSettings settings, CancellationToken token)
        {
            lock (ensureSync)
            {
                if (inFlightEnsureTask != null && !inFlightEnsureTask.IsCompleted)
                {
                    return inFlightEnsureTask; // 复用正在进行的任务
                }

                // 创建新的执行任务
                inFlightEnsureTask = EnsureServerRunningCoreAsync(settings, token);
                return inFlightEnsureTask;
            }
        }

        static async Task<bool> EnsureServerRunningCoreAsync(UserSettings settings, CancellationToken token)
        {
            // 再检查一次健康（可能在排队期间被其它实例启动）
            if (await IsHealthyAsync(settings, token).ConfigureAwait(false))
            {
                lastHealthyUtc = DateTime.UtcNow;
                return true;
            }

            // 情况: 已持有 singleInstanceMutex (说明我们是最初启动者) 但进程被手动终止。
            // 尝试在不释放互斥的情况下直接重启一次，成功则继续持有；失败再释放让后续调用重新竞争启动。
            if (singleInstanceMutex != null)
            {
                bool restarted = await TryRestartOwnedMutexServerAsync(settings, token).ConfigureAwait(false);
                if (restarted)
                {
                    lastHealthyUtc = DateTime.UtcNow;
                    return true;
                }
                
                // 重启失败则释放互斥，允许后续重新获取并再试
                try { singleInstanceMutex.ReleaseMutex(); } catch { }
                try { singleInstanceMutex.Dispose(); } catch { }
                singleInstanceMutex = null;
            }

            bool created;
            try
            {
                singleInstanceMutex = new Mutex(initiallyOwned: true, name: MutexName, createdNew: out created);
            }
            catch { created = false; }

            if (created)
            {
                // Double check
                if (!await IsHealthyAsync(settings, token).ConfigureAwait(false))
                {
                    StartProcess(settings);
                    for (int i = 0; i < 10; i++)
                    {
                        await Task.Delay(500, token).ConfigureAwait(false);
                        if (await IsHealthyAsync(settings, token).ConfigureAwait(false))
                        {
                            lastHealthyUtc = DateTime.UtcNow;
                            return true;
                        }
                    }
                }
                var healthy = await IsHealthyAsync(settings, token).ConfigureAwait(false);
                if (healthy)
                {
                    lastHealthyUtc = DateTime.UtcNow;
                    return true;
                }
                // 启动失败，释放互斥
                try { singleInstanceMutex?.ReleaseMutex(); } catch { }
                try { singleInstanceMutex?.Dispose(); } catch { }
                singleInstanceMutex = null;
                return false;
            }
            // 其他实例已在启动 -> 轮询等待
            for (int i2 = 0; i2 < 10; i2++)
            {
                await Task.Delay(500, token).ConfigureAwait(false);
                if (await IsHealthyAsync(settings, token).ConfigureAwait(false))
                {
                    lastHealthyUtc = DateTime.UtcNow;
                    return true;
                }
            }
            return false;
        }

        static async Task<bool> TryRestartOwnedMutexServerAsync(UserSettings settings, CancellationToken token)
        {
            try
            {
                if (await IsHealthyAsync(settings, token).ConfigureAwait(false)) return true; // 已恢复
                StartProcess(settings);
                for (int i = 0; i < 10; i++)
                {
                    await Task.Delay(500, token).ConfigureAwait(false);
                    if (await IsHealthyAsync(settings, token).ConfigureAwait(false)) return true;
                }
            }
            catch { }
            return false;
        }

        public static async Task<bool> IsHealthyAsync(UserSettings settings, CancellationToken token)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(settings.LocalServiceUrl)) return false;
                var url = settings.LocalServiceUrl.TrimEnd('/') + "/api/Lucene/GetIndexViewList";
                using (var cts = CancellationTokenSource.CreateLinkedTokenSource(token))
                {
                    cts.CancelAfter(TimeSpan.FromSeconds(2));
                    var resp = await http.GetAsync(url, cts.Token).ConfigureAwait(false);
                    return resp.IsSuccessStatusCode;
                }
            }
            catch { return false; }
        }

        public static Task<bool> StopServerIfLastAsync(UserSettings settings)
        {
            if (settings == null || settings.Mode != ServerMode.Local) return Task.FromResult(false);
            try
            {
                using (var global = new Mutex(false, GlobalClientsMutexName))
                {
                    global.WaitOne();
                    try
                    {
                        RemoveOwnClientLock_NoThrow(settings);
                        var anyOthers = AnyOtherAliveClient_NoThrow(settings);
                        if (!anyOthers)
                        {
                            TryStopServerProcess(settings);
                            return Task.FromResult(true);
                        }
                    }
                    finally { try { global.ReleaseMutex(); } catch { } }
                }
            }
            catch { }
            return Task.FromResult(false);
        }

        public static void ForceStop(UserSettings settings)
        {
            TryStopServerProcess(settings);
        }

        static void StartProcess(UserSettings settings)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(settings.LocalServerInstallPath)) return;
                var exe = Directory.EnumerateFiles(settings.LocalServerInstallPath, "CodeIndex.Server*.exe", SearchOption.TopDirectoryOnly)
                                   .OrderByDescending(f => f.Length)
                                   .FirstOrDefault();
                if (exe == null || !File.Exists(exe)) return;

                var luceneDir = settings.LocalServerDataDirectory;
                if (string.IsNullOrWhiteSpace(luceneDir))
                {
                    luceneDir = Path.Combine(settings.LocalServerInstallPath, "Data");
                }
                Directory.CreateDirectory(luceneDir);

                var args = $"--CodeIndex:LuceneIndex=\"{luceneDir}\"";
                if (!string.IsNullOrWhiteSpace(settings.LocalServiceUrl))
                {
                    args += $" --urls=\"{settings.LocalServiceUrl}\"";
                }
                var psi = new ProcessStartInfo(exe, args)
                {
                    WorkingDirectory = settings.LocalServerInstallPath,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                var p = Process.Start(psi);
                if (p != null)
                {
                    try
                    {
                        var pidFile = Path.Combine(settings.LocalServerInstallPath, "server.pid");
                        File.WriteAllText(pidFile, p.Id.ToString());
                    }
                    catch { }
                }
            }
            catch { }
        }

        static void TryStopServerProcess(UserSettings settings)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(settings.LocalServerInstallPath)) return;
                // 优先使用 PID 文件
                var pidFile = Path.Combine(settings.LocalServerInstallPath, "server.pid");
                bool stopped = false;
                if (File.Exists(pidFile))
                {
                    try
                    {
                        var txt = File.ReadAllText(pidFile).Trim();
                        if (int.TryParse(txt, out var pid))
                        {
                            try
                            {
                                var proc = Process.GetProcessById(pid);
                                // 双重确认路径，避免误杀
                                var exePath = proc?.MainModule?.FileName;
                                if (!string.IsNullOrEmpty(exePath) && File.Exists(exePath) && exePath.IndexOf("CodeIndex.Server", StringComparison.OrdinalIgnoreCase) >= 0)
                                {
                                    proc.Kill();
                                    stopped = true;
                                }
                            }
                            catch { }
                        }
                    }
                    catch { }
                }
                if (!stopped)
                {
                    // 回退：遍历所有进程匹配可执行路径
                    var exeCandidates = Directory.GetFiles(settings.LocalServerInstallPath, "CodeIndex.Server*.exe", SearchOption.TopDirectoryOnly);
                    foreach (var proc in Process.GetProcesses())
                    {
                        try
                        {
                            var module = proc.MainModule; // 可能抛异常 (访问权限)
                            if (module == null) continue;
                            if (exeCandidates.Any(c => string.Equals(c, module.FileName, StringComparison.OrdinalIgnoreCase)))
                            {
                                proc.Kill();
                            }
                        }
                        catch { /* ignore */ }
                    }
                }
            }
            catch { }
            finally
            {
                try { singleInstanceMutex?.ReleaseMutex(); singleInstanceMutex?.Dispose(); singleInstanceMutex = null; } catch { }
            }
        }

        #region Client lock helpers
        static void EnsureClientLockFile(UserSettings settings)
        {
            if (settings == null) return;
            if (clientLockStream != null) return; // 已创建
            if (string.IsNullOrWhiteSpace(settings.LocalServerInstallPath)) return;
            try
            {
                using (var global = new Mutex(false, GlobalClientsMutexName))
                {
                    global.WaitOne();
                    try
                    {
                        var folder = Path.Combine(settings.LocalServerInstallPath, ClientLocksFolderName);
                        Directory.CreateDirectory(folder);
                        // 生成唯一文件: <pid>-<guid>.lock 内容写入时间戳
                        var pid = Process.GetCurrentProcess().Id;
                        clientLockFilePath = Path.Combine(folder, pid + "-" + Guid.NewGuid().ToString("N") + ".lock");
                        clientLockStream = new FileStream(clientLockFilePath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.Read);
                        // .NET Framework 没有只有 (Stream, bool leaveOpen) 的构造，需要显式提供 Encoding 与 bufferSize
                        using (var sw = new StreamWriter(clientLockStream, Encoding.UTF8, 1024, leaveOpen: true))
                        {
                            sw.WriteLine(pid);
                            sw.WriteLine(DateTime.UtcNow.ToString("o"));
                            sw.Flush();
                        }
                    }
                    finally { try { global.ReleaseMutex(); } catch { } }
                }
            }
            catch { /* 忽略 */ }
        }

        static void RemoveOwnClientLock_NoThrow(UserSettings settings)
        {
            try
            {
                clientLockStream?.Dispose();
                clientLockStream = null;
                if (!string.IsNullOrEmpty(clientLockFilePath) && File.Exists(clientLockFilePath))
                {
                    File.Delete(clientLockFilePath);
                }
            }
            catch { }
        }

        static bool AnyOtherAliveClient_NoThrow(UserSettings settings)
        {
            try
            {
                if (settings == null || string.IsNullOrWhiteSpace(settings.LocalServerInstallPath)) return false;
                var folder = Path.Combine(settings.LocalServerInstallPath, ClientLocksFolderName);
                if (!Directory.Exists(folder)) return false; // 无其它
                var files = Directory.GetFiles(folder, "*.lock", SearchOption.TopDirectoryOnly);
                bool foundOther = false;
                foreach (var f in files)
                {
                    if (string.Equals(f, clientLockFilePath, StringComparison.OrdinalIgnoreCase)) continue; // 自己已删除或即将删除
                    int pid = ParsePidFromFileName(f);
                    if (pid <= 0)
                    {
                        TryDeleteFile(f);
                        continue;
                    }
                    if (!ProcessAlive(pid))
                    {
                        // 僵尸文件
                        TryDeleteFile(f);
                        continue;
                    }
                    // 另一个仍然存活的 VS 进程
                    foundOther = true;
                }
                return foundOther;
            }
            catch { }
            return false;
        }

        static int ParsePidFromFileName(string path)
        {
            try
            {
                var name = Path.GetFileNameWithoutExtension(path); // pid-guid
                var dash = name.IndexOf('-');
                if (dash <= 0) return -1;
                if (int.TryParse(name.Substring(0, dash), out var pid)) return pid;
            }
            catch { }
            return -1;
        }

        static bool ProcessAlive(int pid)
        {
            try { Process.GetProcessById(pid); return true; } catch { return false; }
        }

        static void TryDeleteFile(string f)
        {
            try { File.Delete(f); } catch { }
        }
        #endregion
    }
}
