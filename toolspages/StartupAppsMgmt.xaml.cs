using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace SystemInfoViewer.toolspages
{
    #region ����������ģ��
    public class StartupAppInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Publisher { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public bool IsEnabled { get; set; }
        public string Id { get; set; } = string.Empty;
        public string Status => IsEnabled ? "������" : "�ѽ���";
    }
    #endregion

    #region �������������
    public static class StartupAppHelper
    {
        private static readonly (RegistryHive Hive, string Path)[] RegistryStartupPaths = new[]
        {
            (RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Run"),
            (RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\RunOnce"),
            (RegistryHive.LocalMachine, @"Software\Microsoft\Windows\CurrentVersion\Run"),
            (RegistryHive.LocalMachine, @"Software\Microsoft\Windows\CurrentVersion\RunOnce"),
            (RegistryHive.LocalMachine, @"Software\Wow6432Node\Microsoft\Windows\CurrentVersion\Run"),
            (RegistryHive.LocalMachine, @"Software\Wow6432Node\Microsoft\Windows\CurrentVersion\RunOnce")
        };

        public static List<StartupAppInfo> GetStartupApps()
        {
            var startupApps = new List<StartupAppInfo>();
            GetRegistryStartupApps(startupApps);
            GetFolderStartupApps(startupApps);

            return startupApps
                .GroupBy(a => a.Path.ToLowerInvariant())
                .Select(g => g.First())
                .Where(a => !string.IsNullOrEmpty(a.Name))
                .OrderBy(a => a.Name)
                .ToList();
        }

        private static void GetRegistryStartupApps(List<StartupAppInfo> startupApps)
        {
            foreach (var (hive, path) in RegistryStartupPaths)
            {
                try
                {
                    using var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64);
                    using var key = baseKey.OpenSubKey(path);
                    if (key == null) continue;

                    foreach (var valueName in key.GetValueNames())
                    {
                        try
                        {
                            var value = key.GetValue(valueName) as string;
                            if (string.IsNullOrEmpty(value)) continue;

                            var parsedPath = ParseStartupPath(value);
                            startupApps.Add(new StartupAppInfo
                            {
                                Id = $"Registry_{hive}_{path}_{valueName}",
                                Name = string.IsNullOrEmpty(valueName) ? Path.GetFileName(parsedPath) : valueName,
                                Path = parsedPath,
                                Publisher = GetPublisherFromPath(parsedPath),
                                Location = $"ע���: {hive}\\{path}",
                                IsEnabled = true
                            });
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"����ע��������� {valueName} ʧ��: {ex.Message}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"����ע���·�� {hive}\\{path} ʧ��: {ex.Message}");
                }
            }
        }

        private static void GetFolderStartupApps(List<StartupAppInfo> startupApps)
        {
            var startupFolders = new[]
            {
                Environment.GetFolderPath(Environment.SpecialFolder.Startup),
                Environment.GetFolderPath(Environment.SpecialFolder.CommonStartup)
            };

            foreach (var folder in startupFolders)
            {
                try
                {
                    if (!Directory.Exists(folder)) continue;

                    foreach (var shortcutPath in Directory.EnumerateFiles(folder, "*.lnk", SearchOption.TopDirectoryOnly))
                    {
                        try
                        {
                            var targetPath = ShortcutHelper.GetShortcutTarget(shortcutPath);
                            if (string.IsNullOrEmpty(targetPath) || !File.Exists(targetPath)) continue;

                            startupApps.Add(new StartupAppInfo
                            {
                                Id = $"Folder_{shortcutPath}",
                                Name = Path.GetFileNameWithoutExtension(shortcutPath),
                                Path = targetPath,
                                Publisher = GetPublisherFromPath(targetPath),
                                Location = $"�ļ���: {folder}",
                                IsEnabled = true
                            });
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"������ݷ�ʽ {shortcutPath} ʧ��: {ex.Message}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"���������ļ��� {folder} ʧ��: {ex.Message}");
                }
            }
        }

        public static bool SetStartupAppState(StartupAppInfo appInfo, bool enable)
        {
            try
            {
                if (appInfo.Id.StartsWith("Registry_"))
                    return SetRegistryStartupState(appInfo, enable);

                if (appInfo.Id.StartsWith("Folder_"))
                    return SetFolderStartupState(appInfo, enable);

                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"�޸�������״̬ʧ��: {ex.Message}");
                return false;
            }
        }

        private static bool SetRegistryStartupState(StartupAppInfo appInfo, bool enable)
        {
            var idParts = appInfo.Id.Split(new[] { '_' }, 4);
            if (idParts.Length < 4) return false;

            var hive = (RegistryHive)Enum.Parse(typeof(RegistryHive), idParts[1]);
            var path = idParts[2];
            var valueName = idParts[3];
            var disabledName = $"_{valueName}_Disabled";

            using var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64);
            using var key = baseKey.OpenSubKey(path, writable: true);
            if (key == null) return false;

            if (enable)
            {
                if (key.GetValue(disabledName) != null)
                {
                    var value = key.GetValue(disabledName);
                    key.DeleteValue(disabledName);
                    key.SetValue(valueName, value);
                    return true;
                }
            }
            else
            {
                if (key.GetValue(valueName) != null)
                {
                    var value = key.GetValue(valueName);
                    key.DeleteValue(valueName);
                    key.SetValue(disabledName, value);
                    return true;
                }
            }
            return false;
        }

        private static bool SetFolderStartupState(StartupAppInfo appInfo, bool enable)
        {
            var originalPath = appInfo.Id.Substring("Folder_".Length);
            var disabledFolder = Path.Combine(Path.GetDirectoryName(originalPath), "DisabledStartup");

            if (!Directory.Exists(disabledFolder))
                Directory.CreateDirectory(disabledFolder);

            var disabledPath = Path.Combine(disabledFolder, Path.GetFileName(originalPath));

            if (enable)
            {
                if (File.Exists(disabledPath))
                {
                    File.Move(disabledPath, originalPath, overwrite: true);
                    return true;
                }
            }
            else
            {
                if (File.Exists(originalPath))
                {
                    File.Move(originalPath, disabledPath, overwrite: true);
                    return true;
                }
            }
            return false;
        }

        private static string ParseStartupPath(string rawPath)
        {
            if (rawPath.StartsWith("\"") && rawPath.Contains("\""))
                return rawPath.Substring(1, rawPath.IndexOf("\"", 1) - 1);

            return rawPath.Split(' ')[0];
        }

        private static string GetPublisherFromPath(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    var versionInfo = FileVersionInfo.GetVersionInfo(filePath);
                    return !string.IsNullOrEmpty(versionInfo.CompanyName)
                        ? versionInfo.CompanyName
                        : "δ֪������";
                }
            }
            catch { }
            return "δ֪������";
        }
    }
    #endregion

    #region ��ݷ�ʽ������
    public static class ShortcutHelper
    {
        [System.Runtime.InteropServices.DllImport("shell32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        private static extern int SHGetPathFromIDListW(IntPtr pidl, System.Text.StringBuilder pszPath);

        [System.Runtime.InteropServices.DllImport("shell32.dll")]
        private static extern IntPtr SHParseDisplayName(string name, IntPtr bindingContext, out IntPtr pidl, uint sfgaoIn, out uint sfgaoOut);

        public static string GetShortcutTarget(string shortcutPath)
        {
            try
            {
                var sb = new System.Text.StringBuilder(260);
                var hr = SHParseDisplayName(shortcutPath, IntPtr.Zero, out var pidl, 0, out _);
                if (hr == 0 && SHGetPathFromIDListW(pidl, sb) != 0)
                    return sb.ToString();
            }
            catch { }
            return null;
        }
    }
    #endregion

    #region ���������ҳ��
    public sealed partial class StartupAppsMgmt : Page, INotifyPropertyChanged
    {
        private List<StartupAppInfo> _allStartupApps = new List<StartupAppInfo>();
        private ObservableCollection<StartupAppInfo> _filteredStartupApps = new ObservableCollection<StartupAppInfo>();
        private string _searchText = string.Empty;
        private bool _isLoading;

        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<StartupAppInfo> FilteredStartupApps
        {
            get => _filteredStartupApps;
            set
            {
                _filteredStartupApps = value;
                OnPropertyChanged();
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                OnPropertyChanged();
            }
        }

        public StartupAppsMgmt()
        {
            InitializeComponent();
            Loaded += StartupAppsMgmt_Loaded;
        }

        private void StartupAppsMgmt_Loaded(object sender, RoutedEventArgs e)
        {
            _ = LoadStartupAppsAsync();
        }

        #region ���ݼ��ط���
        private async Task LoadStartupAppsAsync()
        {
            try
            {
                IsLoading = true;
                LoadingIndicator.Visibility = Visibility.Visible;
                StartupListScrollViewer.Visibility = Visibility.Collapsed;
                EmptyState.Visibility = Visibility.Collapsed;
                TotalCountText.Text = "������...";

                _allStartupApps = await Task.Run(() => StartupAppHelper.GetStartupApps());
                await UpdateFilteredListAsync(_allStartupApps);

                TotalCountText.Text = $"�� {_allStartupApps.Count} ��������";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"�����������б����: {ex.Message}");
                EmptyState.Visibility = Visibility.Visible;
                TotalCountText.Text = "����ʧ��";
            }
            finally
            {
                IsLoading = false;
                LoadingIndicator.Visibility = Visibility.Collapsed;

                if (FilteredStartupApps.Count == 0)
                    EmptyState.Visibility = Visibility.Visible;
                else
                    StartupListScrollViewer.Visibility = Visibility.Visible;
            }
        }

        private async Task UpdateFilteredListAsync(IEnumerable<StartupAppInfo> items)
        {
            FilteredStartupApps.Clear();

            const int batchSize = 50;
            var batches = items.Where(FilterStartupApp).Batch(batchSize);

            foreach (var batch in batches)
            {
                foreach (var item in batch)
                    FilteredStartupApps.Add(item);

                await Task.Delay(10);
            }
        }

        private bool FilterStartupApp(StartupAppInfo app)
        {
            if (string.IsNullOrWhiteSpace(_searchText))
                return true;

            return (app.Name?.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                   (app.Publisher?.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                   (app.Path?.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                   (app.Location?.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ?? false);
        }
        #endregion

        #region �¼�������
        private async void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _searchText = SearchBox.Text?.Trim() ?? string.Empty;
            if (_allStartupApps == null || _allStartupApps.Count == 0)
                return;

            var currentSearch = _searchText;
            await Task.Delay(200);
            if (currentSearch != _searchText) return;

            await UpdateFilteredListAsync(_allStartupApps);

            var count = FilteredStartupApps.Count;
            TotalCountText.Text = string.IsNullOrWhiteSpace(_searchText)
                ? $"�� {_allStartupApps.Count} ��������"
                : $"�� {_allStartupApps.Count} ��������ҵ� {count} ��ƥ����";

            EmptyState.Visibility = count == 0 ? Visibility.Visible : Visibility.Collapsed;
            StartupListScrollViewer.Visibility = count > 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            await LoadStartupAppsAsync();
        }

        private async void ToggleSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleSwitch toggleSwitch && toggleSwitch.DataContext is StartupAppInfo appInfo)
            {
                bool newState = toggleSwitch.IsOn;
                bool success = await Task.Run(() => StartupAppHelper.SetStartupAppState(appInfo, newState));

                if (!success)
                {
                    toggleSwitch.IsOn = !newState;
                    await new ContentDialog
                    {
                        Title = "����ʧ��",
                        Content = $"�޷�{(newState ? "����" : "����")}{appInfo.Name}��������Ҫ����ԱȨ�ޡ�",
                        CloseButtonText = "ȷ��",
                        XamlRoot = this.XamlRoot
                    }.ShowAsync();
                }
            }
        }

        private void ToolsLink_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.NavigationService.Instance.Navigate(typeof(ToolsPage));
        }
        #endregion

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
    #endregion
}