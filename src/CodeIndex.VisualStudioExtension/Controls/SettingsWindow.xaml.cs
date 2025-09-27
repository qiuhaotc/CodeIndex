using System.Windows;
using CodeIndex.VisualStudioExtension.Models;

namespace CodeIndex.VisualStudioExtension.Controls
{
    public partial class SettingsWindow : Window
    {
        public SettingsWindow(SettingsViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;
        }
    }
}
