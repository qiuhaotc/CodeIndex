using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;

namespace CodeIndex.VisualStudioExtension
{
    /// <summary>
    /// This class implements the tool window exposed by this package and hosts a user control.
    /// </summary>
    /// <remarks>
    /// In Visual Studio tool windows are composed of a frame (implemented by the shell) and a pane,
    /// usually implemented by the package implementer.
    /// <para>
    /// This class derives from the ToolWindowPane class provided from the MPF in order to use its
    /// implementation of the IVsUIElementPane interface.
    /// </para>
    /// </remarks>
    [Guid("6d5fde57-9331-4f68-8c3e-2945d8049f40")]
    public class CodeIndexSearchWindow : ToolWindowPane
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CodeIndexSearchWindow"/> class.
        /// </summary>
        public CodeIndexSearchWindow() : base(null)
        {
            this.Caption = "Code Index Search";

            // This is the user control hosted by the tool window; Note that, even if this class implements IDisposable,
            // we are not calling Dispose on this object. This is because ToolWindowPane calls Dispose on
            // the object returned by the Content property.
            this.Content = new CodeIndexSearchWindowControl();
        }
    }
}
