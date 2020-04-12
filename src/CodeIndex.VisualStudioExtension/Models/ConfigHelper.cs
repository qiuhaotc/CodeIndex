using System.Configuration;
using System.IO;

namespace CodeIndex.VisualStudioExtension
{
    public static class ConfigHelper
    {
        static Configuration configuration;
        public static Configuration Configuration
        {
            get
            {
                if (configuration == null)
                {
                    var location = System.Reflection.Assembly.GetExecutingAssembly().Location;
                    var fileInfo = new FileInfo(location);
                    if (fileInfo.Exists)
                    {
                        var configFileMap = new ExeConfigurationFileMap();
                        configFileMap.ExeConfigFilename = Path.Combine(fileInfo.DirectoryName, "CodeIndex.Settings.config");

                        configuration = ConfigurationManager.OpenMappedExeConfiguration(configFileMap, ConfigurationUserLevel.None);
                    }
                }

                return configuration;
            }
        }

        public static bool SetConfiguration(string key, string value)
        {
            if(Configuration == null)
            {
                return false;
            }

            try
            {
                if (Configuration.AppSettings.Settings[key] != null)
                {
                    Configuration.AppSettings.Settings[key].Value = value;
                }
                else
                {
                    Configuration.AppSettings.Settings.Add(key, value);
                }

                Configuration.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection("appSettings");
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
