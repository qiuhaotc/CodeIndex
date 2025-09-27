using System;
using System.ComponentModel.Design;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

namespace CodeIndex.VisualStudioExtension
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class CodeIndexSearchWindowCommand
    {
        /// <summary>
        /// Command ID.
        /// </summary>
    public const int CommandId = 4129;
    public const int OpenSettingsCommandId = 4130;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("c025b4ef-e6ed-42b4-9c22-2d6835421d25");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly AsyncPackage package;

        /// <summary>
        /// Initializes a new instance of the <see cref="CodeIndexSearchWindowCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private CodeIndexSearchWindowCommand(AsyncPackage package, OleMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new MenuCommand(this.Execute, menuCommandID);
            commandService.AddCommand(menuItem);

            var openSettingsCommandID = new CommandID(CommandSet, OpenSettingsCommandId);
            var openSettingsMenuItem = new MenuCommand(this.OpenSettingsExecute, openSettingsCommandID);
            commandService.AddCommand(openSettingsMenuItem);
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static CodeIndexSearchWindowCommand Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private Microsoft.VisualStudio.Shell.IAsyncServiceProvider ServiceProvider
        {
            get
            {
                return this.package;
            }
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static async Task InitializeAsync(AsyncPackage package)
        {
            // Switch to the main thread - the call to AddCommand in CodeIndexSearchWindowCommand's constructor requires
            // the UI thread.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync((typeof(IMenuCommandService))) as OleMenuCommandService;
            Instance = new CodeIndexSearchWindowCommand(package, commandService);
        }

        /// <summary>
        /// Shows the tool window when the menu item is clicked.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        private void Execute(object sender, EventArgs e)
        {
            this.package.JoinableTaskFactory.RunAsync(async delegate
            {
                ToolWindowPane window = await this.package.ShowToolWindowAsync(typeof(CodeIndexSearchWindow), 0, true, this.package.DisposalToken);
                if ((null == window) || (null == window.Frame))
                {
                    throw new NotSupportedException("Cannot create tool window");
                }
            });
        }

        private void OpenSettingsExecute(object sender, EventArgs e)
        {
            this.package.JoinableTaskFactory.RunAsync(async delegate
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                try
                {
                    var settings = UserSettingsManager.Load(); // 确保已初始化目录
                    var path = UserSettingsManager.SettingsFile;
                    if (!System.IO.File.Exists(path))
                    {
                        UserSettingsManager.Save(settings); // 创建文件
                    }

                    var dte = (EnvDTE.DTE)await package.GetServiceAsync(typeof(EnvDTE.DTE));
                    dte.ItemOperations.OpenFile(path);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show("Open settings failed: " + ex.Message, "CodeIndex", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            });
        }
    }
}
