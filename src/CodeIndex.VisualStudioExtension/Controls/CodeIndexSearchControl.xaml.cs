using System.Windows.Controls;
using System.Windows.Input;

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
                if(e.KeyboardDevice.Modifiers != ModifierKeys.Control)
                {
                    SearchButton.Command?.Execute(null);
                }
            }
        }
    }
}
