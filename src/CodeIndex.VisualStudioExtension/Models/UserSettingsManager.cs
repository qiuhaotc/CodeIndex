using System;
using System.IO;
using Newtonsoft.Json;

namespace CodeIndex.VisualStudioExtension
{
    public enum ServerMode
    {
        Remote = 0,
        Local = 1
    }

    public class UserSettings
    {
        // 旧版本字段：仍然保留用于向后兼容（Remote 模式 URL）
        public string ServiceUrl { get; set; } = "http://localhost:5000";

        // 新增：区分当前服务器模式
        public ServerMode Mode { get; set; } = ServerMode.Remote;

        // 远程服务器 URL（如果与旧 ServiceUrl 不同，可独立设置）
        public string RemoteServiceUrl { get; set; } = "http://localhost:5000";

        // 本地服务器监听 URL（下载/启动后可固定，如 http://localhost:58080）
        public string LocalServiceUrl { get; set; } = "http://localhost:58080";

        // 本地服务器安装根目录（下载、解压存放位置）
        public string LocalServerInstallPath { get; set; } = string.Empty;

        // 本地服务器数据目录（索引数据）
        public string LocalServerDataDirectory { get; set; } = string.Empty;

        // 记录最近一次成功安装的服务器版本（用于判断是否需要重新下载）
        public string LocalServerVersion { get; set; } = string.Empty;
    }

    internal static class UserSettingsManager
    {
        static readonly object Locker = new();
        static UserSettings cached;
        internal static string SettingsDirectory => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CodeIndex.VisualStudioExtension");
        internal static string SettingsFile => Path.Combine(SettingsDirectory, "codeindex.user.settings.json");

        public static UserSettings Load()
        {
            lock (Locker)
            {
                if (cached != null)
                {
                    return cached;
                }

                try
                {
                    if (File.Exists(SettingsFile))
                    {
                        cached = JsonConvert.DeserializeObject<UserSettings>(File.ReadAllText(SettingsFile)) ?? new UserSettings();
                        PostLoadBackFill(cached);
                    }
                    else
                    {
                        cached = new UserSettings();
                        // 兼容旧版本：尝试迁移原有配置文件中的 ServiceUrl
                        try
                        {
                            var legacy = ConfigHelper.Configuration?.AppSettings?.Settings?[nameof(UserSettings.ServiceUrl)]?.Value;
                            if (!string.IsNullOrWhiteSpace(legacy))
                            {
                                cached.ServiceUrl = legacy;
                                cached.RemoteServiceUrl = legacy; // 同步到新字段
                                Save(cached); // 立即保存迁移
                            }
                        }
                        catch { /* 忽略迁移异常 */ }
                        PostLoadBackFill(cached);
                    }
                }
                catch
                {
                    cached = new UserSettings();
                    PostLoadBackFill(cached);
                }

                return cached;
            }
        }

        public static void Save(UserSettings settings)
        {
            lock (Locker)
            {
                try
                {
                    Directory.CreateDirectory(SettingsDirectory);
                    File.WriteAllText(SettingsFile, JsonConvert.SerializeObject(settings, Formatting.Indented));
                    cached = settings;
                }
                catch
                {
                    // 记录日志可选：当前扩展无集中日志设施
                }
            }
        }

        static void PostLoadBackFill(UserSettings s)
        {
            // Back-fill 逻辑：旧版本只有 ServiceUrl
            if (string.IsNullOrWhiteSpace(s.RemoteServiceUrl))
            {
                s.RemoteServiceUrl = s.ServiceUrl;
            }
            if (string.IsNullOrWhiteSpace(s.LocalServiceUrl))
            {
                s.LocalServiceUrl = "http://localhost:58080";
            }
        }
    }
}