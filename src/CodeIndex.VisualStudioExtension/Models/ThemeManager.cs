using System;
using System.Windows;
using System.Windows.Media;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;

namespace CodeIndex.VisualStudioExtension
{
    /// <summary>
    /// Manages theme changes and provides theme-aware colors
    /// </summary>
    public static class ThemeManager
    {
        private static bool _initialized = false;

        /// <summary>
        /// Initialize theme monitoring
        /// </summary>
        public static void Initialize()
        {
            if (_initialized) return;

            ThreadHelper.ThrowIfNotOnUIThread();
            
            // Subscribe to VS theme changes
            VSColorTheme.ThemeChanged += OnThemeChanged;
            _initialized = true;
            
            // Apply current theme
            ApplyCurrentTheme();
        }

        private static void OnThemeChanged(ThemeChangedEventArgs e)
        {
            ThreadHelper.JoinableTaskFactory.Run(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                ApplyCurrentTheme();
            });
        }

        private static void ApplyCurrentTheme()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                // Get the current VS theme colors and convert to WPF colors
                var backgroundColor = ConvertToWpfColor(VSColorTheme.GetThemedColor(EnvironmentColors.ToolWindowBackgroundColorKey));
                var foregroundColor = ConvertToWpfColor(VSColorTheme.GetThemedColor(EnvironmentColors.ToolWindowTextColorKey));
                var inputBackgroundColor = ConvertToWpfColor(VSColorTheme.GetThemedColor(EnvironmentColors.ComboBoxBackgroundColorKey));
                var inputForegroundColor = ConvertToWpfColor(VSColorTheme.GetThemedColor(EnvironmentColors.ComboBoxTextColorKey));
                var selectionColor = ConvertToWpfColor(VSColorTheme.GetThemedColor(EnvironmentColors.SystemHighlightColorKey));
                var headerBackgroundColor = ConvertToWpfColor(VSColorTheme.GetThemedColor(EnvironmentColors.CommandBarGradientBeginColorKey));

                var backgroundBrush = new SolidColorBrush(backgroundColor);
                var foregroundBrush = new SolidColorBrush(foregroundColor);
                var inputBackgroundBrush = new SolidColorBrush(inputBackgroundColor);
                var inputForegroundBrush = new SolidColorBrush(inputForegroundColor);
                var selectionBrush = new SolidColorBrush(selectionColor);
                var headerBackgroundBrush = new SolidColorBrush(headerBackgroundColor);

                // Apply to application resources
                Application.Current?.Resources?.Remove("VsBackgroundBrush");
                Application.Current?.Resources?.Remove("VsForegroundBrush");
                Application.Current?.Resources?.Remove("VsInputBackgroundBrush");
                Application.Current?.Resources?.Remove("VsInputForegroundBrush");
                Application.Current?.Resources?.Remove("VsSelectionBrush");
                Application.Current?.Resources?.Remove("VsHeaderBackgroundBrush");

                Application.Current?.Resources?.Add("VsBackgroundBrush", backgroundBrush);
                Application.Current?.Resources?.Add("VsForegroundBrush", foregroundBrush);
                Application.Current?.Resources?.Add("VsInputBackgroundBrush", inputBackgroundBrush);
                Application.Current?.Resources?.Add("VsInputForegroundBrush", inputForegroundBrush);
                Application.Current?.Resources?.Add("VsSelectionBrush", selectionBrush);
                Application.Current?.Resources?.Add("VsHeaderBackgroundBrush", headerBackgroundBrush);
            }
            catch (Exception ex)
            {
                // Log error but don't crash
                System.Diagnostics.Debug.WriteLine($"ThemeManager error: {ex.Message}");
            }
        }

        /// <summary>
        /// Convert System.Drawing.Color to System.Windows.Media.Color
        /// </summary>
        private static System.Windows.Media.Color ConvertToWpfColor(System.Drawing.Color drawingColor)
        {
            return System.Windows.Media.Color.FromArgb(
                drawingColor.A,
                drawingColor.R,
                drawingColor.G,
                drawingColor.B);
        }

        /// <summary>
        /// Clean up theme monitoring
        /// </summary>
        public static void Cleanup()
        {
            if (_initialized)
            {
                VSColorTheme.ThemeChanged -= OnThemeChanged;
                _initialized = false;
            }
        }
    }
}