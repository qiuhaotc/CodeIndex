using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CodeIndex.Common;
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

        void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (e.KeyboardDevice.Modifiers != ModifierKeys.Control)
                {
                    SearchButton.Command?.Execute(null);
                }
            }
            else
            {
                (DataContext as CodeIndexSearchViewModel)?.GetHintWords();
            }
        }

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
                    System.Windows.Forms.MessageBox.Show("This file is not on your local");
                }
            }
        }
    }
}
