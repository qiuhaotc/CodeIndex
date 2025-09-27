using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

namespace CodeIndex.VisualStudioExtension.Models
{
    internal static class LocalServerLauncher
    {
        static Mutex singleInstanceMutex;
        const string MutexName = "Global_CodeIndex_Server_Singleton"; // 跨 VS 实例共享

        public static void TryLaunch(UserSettings settings)
        {
            if (settings == null) return;
            if (settings.Mode != ServerMode.Local) return;

            try
            {
                // 检查路径 & 主程序可执行文件
                if (string.IsNullOrWhiteSpace(settings.LocalServerInstallPath)) return;

                var exe = Directory.EnumerateFiles(settings.LocalServerInstallPath, "CodeIndex.Server*.exe", SearchOption.TopDirectoryOnly)
                                    .OrderByDescending(f => f.Length) // 简单挑一个（发布单文件场景 应该只有一个）
                                    .FirstOrDefault();
                if (exe == null || !File.Exists(exe)) return;

                // 简易互斥确保只启动一次
                var created = false;
                singleInstanceMutex = new Mutex(initiallyOwned: true, name: MutexName, createdNew: out created);
                if (!created)
                {
                    // 已有实例，直接返回（不再检测进程列表）。
                    return;
                }

                // 启动参数：覆盖 LuceneIndex（如果配置了数据目录则优先数据目录，否则回退 LuceneIndex）
                var luceneDir = settings.LocalServerDataDirectory;
                if (string.IsNullOrWhiteSpace(luceneDir))
                {
                    // 没有单独数据目录则允许用户直接把 InstallPath/Lucene 作为索引根
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

                Process.Start(psi);
            }
            catch
            {
                // 忽略异常（可扩展日志）
            }
        }
    }
}
