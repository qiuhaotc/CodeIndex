using CodeIndex.VisualStudioExtension.Services;

namespace CodeIndex.VisualStudioExtension.Converts
{
    /// <summary>
    /// Helper class to expose LocalizationService for XAML binding
    /// </summary>
    public static class LocalizationHelper
    {
        public static LocalizationService Instance => LocalizationService.Instance;
    }
}
