using System;
using System.Globalization;
using System.Windows.Data;
using CodeIndex.VisualStudioExtension.Services;

namespace CodeIndex.VisualStudioExtension.Converts
{
    /// <summary>
    /// Converts a resource key to localized string for XAML binding.
    /// Usage: {Binding Converter={StaticResource LocalizeConverter}, ConverterParameter=ResourceKey}
    /// </summary>
    public class LocalizeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter is string key)
            {
                return LocalizationService.Instance.GetString(key);
            }

            // If value is the key (for direct binding scenarios)
            if (value is string keyValue)
            {
                return LocalizationService.Instance.GetString(keyValue);
            }

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
