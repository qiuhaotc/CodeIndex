using System;
using System.IO;
using System.Windows.Controls;
using System.Windows.Input;
using CodeIndex.VisualStudioExtension.Resources;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

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
                return;
            }

            if (SearchViewModel == null)
            {
                return;
            }

            // 调度提示词获取（由 ViewModel 内部使用 JoinableTaskCollection 追踪，避免 VSSDK007 警告）
            SearchViewModel.ScheduleGetHintWords();
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
                    var result = VsShellUtilities.ShowMessageBox(
                        ServiceProvider.GlobalProvider,
                        Strings.Message_FileNotLocal,
                        Strings.Message_FileNotLocalTitle,
                        OLEMSGICON.OLEMSGICON_QUERY,
                        OLEMSGBUTTON.OLEMSGBUTTON_YESNO,
                        OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);

                    if (result == 6) // IDYES
                    {
                        System.Diagnostics.Process.Start($"{SearchViewModel.ServiceUrl}/Details/{codeSourceWithMatchedLine.CodeSource.CodePK}/{SearchViewModel.IndexPk}/{System.Web.HttpUtility.UrlEncode(SearchViewModel.Content)}/{SearchViewModel.CaseSensitive}/{SearchViewModel.PhaseQuery}");
                    }
                }
            }
        }
    }
}
