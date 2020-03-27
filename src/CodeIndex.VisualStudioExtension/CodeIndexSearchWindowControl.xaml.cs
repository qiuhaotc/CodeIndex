using System.Windows.Controls;

namespace CodeIndex.VisualStudioExtension
{
    /// <summary>
    /// Interaction logic for CodeIndexSearchWindowControl.
    /// </summary>
    public partial class CodeIndexSearchWindowControl : UserControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CodeIndexSearchWindowControl"/> class.
        /// </summary>
        public CodeIndexSearchWindowControl()
        {
            InitializeComponent();

            SearchControl.DataContext = new CodeIndexSearchViewModel();
        }
    }
}