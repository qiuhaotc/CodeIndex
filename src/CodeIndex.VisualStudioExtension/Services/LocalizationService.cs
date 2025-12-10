using System;
using System.Resources;

namespace CodeIndex.VisualStudioExtension.Services
{
    /// <summary>
    /// Provides localization services for the extension.
    /// Automatically detects Visual Studio's UI language and loads the appropriate resource file.
    /// </summary>
    public class LocalizationService
    {
        private static readonly Lazy<LocalizationService> instance =
            new Lazy<LocalizationService>(() => new LocalizationService());

        private readonly ResourceManager resourceManager;

        private LocalizationService()
        {
            // Initialize ResourceManager pointing to default resource file
            resourceManager = new ResourceManager(
                "CodeIndex.VisualStudioExtension.Resources.Strings",
                typeof(LocalizationService).Assembly);
        }

        /// <summary>
        /// Gets the singleton instance of LocalizationService.
        /// </summary>
        public static LocalizationService Instance => instance.Value;

        /// <summary>
        /// Indexer to access localized strings by key.
        /// </summary>
        /// <param name="key">Resource key</param>
        /// <returns>Localized string, or key if not found</returns>
        public string this[string key] => GetString(key);

        /// <summary>
        /// Gets a localized string by key.
        /// </summary>
        /// <param name="key">Resource key</param>
        /// <returns>Localized string, or key if not found</returns>
        public string GetString(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return string.Empty;

            try
            {
                var result = resourceManager.GetString(key);
                return result ?? key; // Fallback to key if resource not found
            }
            catch
            {
                return key; // Fallback to key on error
            }
        }

        /// <summary>
        /// Gets a formatted localized string by key with arguments.
        /// </summary>
        /// <param name="key">Resource key</param>
        /// <param name="args">Format arguments</param>
        /// <returns>Formatted localized string</returns>
        public string GetString(string key, params object[] args)
        {
            var format = GetString(key);
            try
            {
                return string.Format(format, args);
            }
            catch
            {
                return format; // Return unformatted string if formatting fails
            }
        }
    }
}
