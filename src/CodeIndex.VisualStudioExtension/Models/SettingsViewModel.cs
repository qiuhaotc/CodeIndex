using System;
using System.ComponentModel;
using System.IO;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Win32;

namespace CodeIndex.VisualStudioExtension.Models
{
    public class SettingsViewModel : INotifyPropertyChanged
    {
        readonly UserSettings settings;
        bool isBusy;

        public SettingsViewModel(UserSettings settings)
        {
            this.settings = settings;
            RemoteServiceUrl = settings.RemoteServiceUrl;
            LocalServiceUrl = settings.LocalServiceUrl;
            LocalServerInstallPath = settings.LocalServerInstallPath;
            LocalServerDataDirectory = settings.LocalServerDataDirectory;
            LocalServerVersion = settings.LocalServerVersion;
            IsLocalMode = settings.Mode == ServerMode.Local;
            IsRemoteMode = settings.Mode == ServerMode.Remote;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        void Raise([CallerMemberName] string p = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));

        public bool IsRemoteMode
        {
            get => !IsLocalMode;
            set
            {
                if (value)
                {
                    IsLocalMode = false;
                    Raise();
                    Raise(nameof(IsLocalMode));
                }
            }
        }

        bool isLocalMode;
        public bool IsLocalMode
        {
            get => isLocalMode;
            set
            {
                if (isLocalMode != value)
                {
                    isLocalMode = value;
                    Raise();
                    Raise(nameof(IsRemoteMode));
                }
            }
        }

        public string RemoteServiceUrl { get; set; }
        public string LocalServiceUrl { get; set; }
        public string LocalServerInstallPath { get; set; }
        public string LocalServerDataDirectory { get; set; }
        public string LocalServerVersion { get; set; }

        public ICommand BrowseInstallPathCommand => new CommonCommand(_ =>
        {
            var dlg = new System.Windows.Forms.FolderBrowserDialog();
            if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                LocalServerInstallPath = dlg.SelectedPath;
                Raise(nameof(LocalServerInstallPath));
            }
        }, _ => true);

        public ICommand BrowseDataDirCommand => new CommonCommand(_ =>
        {
            var dlg = new System.Windows.Forms.FolderBrowserDialog();
            if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                LocalServerDataDirectory = dlg.SelectedPath;
                Raise(nameof(LocalServerDataDirectory));
            }
        }, _ => true);

        public ICommand DownloadOrUpdateCommand => new AsyncCommand(DownloadOrUpdateAsync, () => !isBusy, null);
        public ICommand SaveCommand => new CommonCommand(Save, _ => !isBusy);

        async Task DownloadOrUpdateAsync()
        {
            try
            {
                isBusy = true;
                // 占位：未来实现调用 GitHub API 下载最新 Release
                await Task.Delay(300); // 模拟
            }
            finally
            {
                isBusy = false;
            }
        }

        void Save(object _)
        {
            settings.RemoteServiceUrl = RemoteServiceUrl?.TrimEnd('/');
            settings.LocalServiceUrl = LocalServiceUrl?.TrimEnd('/');
            settings.LocalServerInstallPath = LocalServerInstallPath;
            settings.LocalServerDataDirectory = LocalServerDataDirectory;
            settings.LocalServerVersion = LocalServerVersion;
            settings.Mode = IsLocalMode ? ServerMode.Local : ServerMode.Remote;
            UserSettingsManager.Save(settings);
            CloseWindow(true);
        }

        void CloseWindow(bool dialogResult)
        {
            // 通过打开时的 Window 关联关闭
            foreach (System.Windows.Window w in System.Windows.Application.Current.Windows)
            {
                if (w.DataContext == this)
                {
                    w.DialogResult = dialogResult;
                    w.Close();
                    break;
                }
            }
        }
    }
}
