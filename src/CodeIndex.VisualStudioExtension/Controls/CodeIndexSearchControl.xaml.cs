using System.IO;
using System.Windows.Controls;
using System.Windows.Input;
using EnvDTE;
using Microsoft.VisualStudio.Shell;

namespace CodeIndex.VisualStudioExtension
{
    /// <summary>
    /// Interaction logic for CodeIndexSearchControl.xaml.
    /// </summary>
    [ProvideToolboxControl("CodeIndex.VisualStudioExtension.CodeIndexSearchControl", true)]
    public partial class CodeIndexSearchControl : UserControl
    {
        public CodeIndexSearchControl()
        {
            InitializeComponent();
        }

        void ContentTextBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                TextBox_KeyDown(sender, e);
            }
            else
            {
                SearchViewModel?.GetHintWordsAsync();
            }
        }

        void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (e.KeyboardDevice.Modifiers != ModifierKeys.Control)
                {
                    SearchButton.Command?.Execute(null);
                }
            }
        }

        CodeIndexSearchViewModel SearchViewModel => DataContext as CodeIndexSearchViewModel;

        void Row_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            // Some operations with this row
            if (sender is DataGridRow row && row.Item is CodeSourceWithMatchedLine codeSourceWithMatchedLine)
            {
                if (File.Exists(codeSourceWithMatchedLine.CodeSource.FilePath))
                {
                    var dte = (DTE)Package.GetGlobalService(typeof(DTE));
                    var window = dte.ItemOperations.OpenFile(codeSourceWithMatchedLine.CodeSource.FilePath);
                    (window.Document.Selection as TextSelection)?.GotoLine(codeSourceWithMatchedLine.MatchedLine, true);
                }
                else
                {
                    // TODO: Download to local to open
                    if (System.Windows.MessageBox.Show("This file is not on your local, do you want to open it in the web portal?", "Info", System.Windows.MessageBoxButton.YesNo) == System.Windows.MessageBoxResult.Yes)
                    {
                        System.Diagnostics.Process.Start($"{SearchViewModel.ServiceUrl}/Details/{codeSourceWithMatchedLine.CodeSource.CodePK}/{SearchViewModel.IndexPk}/{System.Web.HttpUtility.UrlEncode(SearchViewModel.Content)}/{SearchViewModel.CaseSensitive}/{SearchViewModel.PhaseQuery}");
                    }
                }
            }
        }
    }
}
