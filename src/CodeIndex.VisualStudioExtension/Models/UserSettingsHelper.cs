using System;
using System.IO;
using Microsoft.Win32;
using Newtonsoft.Json;
using Microsoft.VisualStudio.Shell;

namespace CodeIndex.VisualStudioExtension
{
    /// <summary>
    /// Enhanced configuration helper that provides persistent user settings
    /// </summary>
    public static class UserSettingsHelper
    {
        private const string RegistryKeyPath = @"SOFTWARE\CodeIndex\VisualStudioExtension";
        private const string AppDataFolderName = "CodeIndexVSExtension";
        private static readonly string ConfigFileName = "user-settings.json";
        private static string _configFilePath;

        static UserSettingsHelper()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var configFolderPath = Path.Combine(appDataPath, AppDataFolderName);
            
            if (!Directory.Exists(configFolderPath))
            {
                Directory.CreateDirectory(configFolderPath);
            }

            _configFilePath = Path.Combine(configFolderPath, ConfigFileName);
        }

        /// <summary>
        /// Get a setting value, with fallback to registry and then default value
        /// </summary>
        public static T GetSetting<T>(string key, T defaultValue = default(T))
        {
            try
            {
                // Try JSON file first
                if (TryGetFromJsonFile(key, out T jsonValue))
                {
                    return jsonValue;
                }

                // Fallback to registry
                if (TryGetFromRegistry(key, out T registryValue))
                {
                    // Migration: save to JSON file and remove from registry
                    SetSetting(key, registryValue);
                    RemoveFromRegistry(key);
                    return registryValue;
                }

                // Fallback to legacy ConfigHelper
                var legacyValue = ConfigHelper.Configuration?.AppSettings?.Settings[key]?.Value;
                if (!string.IsNullOrEmpty(legacyValue))
                {
                    try
                    {
                        var convertedValue = (T)Convert.ChangeType(legacyValue, typeof(T));
                        // Migration: save to JSON file
                        SetSetting(key, convertedValue);
                        return convertedValue;
                    }
                    catch
                    {
                        // Ignore conversion errors
                    }
                }

                return defaultValue;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UserSettingsHelper.GetSetting error: {ex.Message}");
                return defaultValue;
            }
        }

        /// <summary>
        /// Set a setting value in JSON file
        /// </summary>
        public static bool SetSetting<T>(string key, T value)
        {
            try
            {
                var settings = LoadSettingsFromFile();
                settings[key] = value;
                SaveSettingsToFile(settings);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UserSettingsHelper.SetSetting error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Remove a setting
        /// </summary>
        public static bool RemoveSetting(string key)
        {
            try
            {
                var settings = LoadSettingsFromFile();
                if (settings.ContainsKey(key))
                {
                    settings.Remove(key);
                    SaveSettingsToFile(settings);
                }
                RemoveFromRegistry(key);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UserSettingsHelper.RemoveSetting error: {ex.Message}");
                return false;
            }
        }

        private static bool TryGetFromJsonFile<T>(string key, out T value)
        {
            value = default(T);
            try
            {
                if (!File.Exists(_configFilePath))
                    return false;

                var settings = LoadSettingsFromFile();
                if (settings.TryGetValue(key, out var objValue))
                {
                    if (objValue is T directValue)
                    {
                        value = directValue;
                        return true;
                    }

                    // Try conversion
                    value = JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(objValue));
                    return true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"TryGetFromJsonFile error: {ex.Message}");
            }
            return false;
        }

        private static bool TryGetFromRegistry<T>(string key, out T value)
        {
            value = default(T);
            try
            {
                using (var regKey = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, false))
                {
                    if (regKey != null)
                    {
                        var regValue = regKey.GetValue(key);
                        if (regValue != null)
                        {
                            value = (T)Convert.ChangeType(regValue, typeof(T));
                            return true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"TryGetFromRegistry error: {ex.Message}");
            }
            return false;
        }

        private static void RemoveFromRegistry(string key)
        {
            try
            {
                using (var regKey = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, true))
                {
                    regKey?.DeleteValue(key, false);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"RemoveFromRegistry error: {ex.Message}");
            }
        }

        private static System.Collections.Generic.Dictionary<string, object> LoadSettingsFromFile()
        {
            if (!File.Exists(_configFilePath))
                return new System.Collections.Generic.Dictionary<string, object>();

            try
            {
                var json = File.ReadAllText(_configFilePath);
                return JsonConvert.DeserializeObject<System.Collections.Generic.Dictionary<string, object>>(json) ?? 
                       new System.Collections.Generic.Dictionary<string, object>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadSettingsFromFile error: {ex.Message}");
                return new System.Collections.Generic.Dictionary<string, object>();
            }
        }

        private static void SaveSettingsToFile(System.Collections.Generic.Dictionary<string, object> settings)
        {
            try
            {
                var json = JsonConvert.SerializeObject(settings, Formatting.Indented);
                File.WriteAllText(_configFilePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SaveSettingsToFile error: {ex.Message}");
            }
        }

        /// <summary>
        /// Export settings to a file for backup
        /// </summary>
        public static bool ExportSettings(string filePath)
        {
            try
            {
                var settings = LoadSettingsFromFile();
                var json = JsonConvert.SerializeObject(settings, Formatting.Indented);
                File.WriteAllText(filePath, json);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ExportSettings error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Import settings from a file
        /// </summary>
        public static bool ImportSettings(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    return false;

                var json = File.ReadAllText(filePath);
                var importedSettings = JsonConvert.DeserializeObject<System.Collections.Generic.Dictionary<string, object>>(json);
                
                if (importedSettings != null)
                {
                    var currentSettings = LoadSettingsFromFile();
                    foreach (var kvp in importedSettings)
                    {
                        currentSettings[kvp.Key] = kvp.Value;
                    }
                    SaveSettingsToFile(currentSettings);
                    return true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ImportSettings error: {ex.Message}");
            }
            return false;
        }
    }
}