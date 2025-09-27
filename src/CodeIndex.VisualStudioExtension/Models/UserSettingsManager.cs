using System;
using System.IO;
using Newtonsoft.Json;

namespace CodeIndex.VisualStudioExtension
{
    internal class UserSettings
    {
        public string ServiceUrl { get; set; } = "http://localhost:5000"; // 默认值，可按需调整
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
                                Save(cached); // 立即保存迁移
                            }
                        }
                        catch { /* 忽略迁移异常 */ }
                    }
                }
                catch
                {
                    cached = new UserSettings();
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
    }
}