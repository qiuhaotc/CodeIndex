using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;

namespace CodeIndex.VisualStudioExtension
{
    public class BaseViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChange([CallerMemberName]string memberName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(memberName));
        }

        public void NotifyPropertyChange(Func<string> propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName.Invoke()));
        }

        /// <summary>
        /// 在 UI 线程执行委托（如果可能）。如果当前不在 UI 线程，会切换。
        /// </summary>
        public static async Task InvokeOnUIThreadAsync(Action action)
        {
            if (action == null) return;
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            action();
        }
    }
}
