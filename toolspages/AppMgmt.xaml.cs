using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace SystemInfoViewer
{
    #region 数据模型
    /// <summary>
    /// 软件信息数据模型类
    /// </summary>
    public class SoftwareInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string Publisher { get; set; } = string.Empty;
        public DateTime? InstallDate { get; set; }
        public string InstallLocation { get; set; } = string.Empty;
        public string UninstallString { get; set; } = string.Empty;

        public string DisplayName => string.IsNullOrEmpty(Name) ? "未知应用" : Name;
        public string FormattedInstallDate => InstallDate.HasValue
            ? InstallDate.Value.ToString("yyyy-MM-dd")
            : "未知日期";
        public bool HasUninstallString => !string.IsNullOrEmpty(UninstallString);
    }
    #endregion

    #region 工具类
    /// <summary>
    /// 软件信息帮助类
    /// 提供获取、解析和操作系统中安装软件的功能
    /// </summary>
    public static class SoftwareInfoHelper
    {
        /// <summary>
        /// 获取系统中已安装的软件列表
        /// </summary>
        public static List<SoftwareInfo> GetInstalledSoftware()
        {
            var softwareList = new List<SoftwareInfo>();

            var registryPaths = new[]
            {
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
                @"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
            };

            foreach (var path in registryPaths)
            {
                try
                {
                    using (var key = Registry.LocalMachine.OpenSubKey(path))
                    {
                        if (key == null)
                        {
                            Debug.WriteLine($"无法打开注册表路径: {path}");
                            continue;
                        }

                        foreach (var subKeyName in key.GetSubKeyNames())
                        {
                            try
                            {
                                using (var subKey = key.OpenSubKey(subKeyName))
                                {
                                    if (subKey != null)
                                    {
                                        var software = GetSoftwareInfoFromRegistry(subKey);
                                        if (software != null)
                                        {
                                            softwareList.Add(software);
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"处理子键 {subKeyName} 时出错: {ex.Message}");
                                continue;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"访问注册表路径 {path} 时出错: {ex.Message}");
                    continue;
                }
            }

            return softwareList
                .GroupBy(s => s.Name)
                .Select(g => g.First())
                .Where(s => !string.IsNullOrEmpty(s.Name))
                .OrderBy(s => s.Name)
                .ToList();
        }

        /// <summary>
        /// 从注册表项获取软件信息
        /// </summary>
        private static SoftwareInfo? GetSoftwareInfoFromRegistry(RegistryKey subKey)
        {
            try
            {
                var name = subKey.GetValue("DisplayName") as string;

                if (string.IsNullOrEmpty(name))
                    return null;

                return new SoftwareInfo
                {
                    Name = name,
                    Version = subKey.GetValue("DisplayVersion") as string ?? string.Empty,
                    Publisher = subKey.GetValue("Publisher") as string ?? string.Empty,
                    InstallDate = ParseInstallDate(subKey.GetValue("InstallDate") as string),
                    InstallLocation = subKey.GetValue("InstallLocation") as string ?? string.Empty,
                    UninstallString = subKey.GetValue("UninstallString") as string ?? string.Empty
                };
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"获取软件信息出错: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 解析安装日期（注册表中的格式通常是yyyyMMdd）
        /// </summary>
        private static DateTime? ParseInstallDate(string? installDateString)
        {
            if (string.IsNullOrEmpty(installDateString))
                return null;

            var cleanDate = installDateString.Trim();

            try
            {
                var formats = new[]
                {
                    "yyyyMMdd",
                    "yyyy-MM-dd",
                    "MM/dd/yyyy",
                    "dd/MM/yyyy",
                    "yyyyMMddHHmmss"
                };

                if (DateTime.TryParseExact(cleanDate, formats,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out DateTime date))
                {
                    return date;
                }

                if (DateTime.TryParse(cleanDate, out date))
                {
                    return date;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"日期解析错误 ({installDateString}): {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// 卸载软件
        /// </summary>
        public static bool UninstallSoftware(string uninstallString)
        {
            if (string.IsNullOrEmpty(uninstallString))
                return false;

            try
            {
                string executable = string.Empty;
                string arguments = string.Empty;

                if (uninstallString.StartsWith("\""))
                {
                    int quoteIndex = uninstallString.IndexOf("\"", 1);
                    if (quoteIndex > 0)
                    {
                        executable = uninstallString.Substring(1, quoteIndex - 1);
                        arguments = uninstallString.Substring(quoteIndex + 1).Trim();
                    }
                }
                else
                {
                    string[] parts = uninstallString.Split(new[] { ' ' }, 2);
                    executable = parts[0];
                    if (parts.Length > 1)
                        arguments = parts[1];
                }

                Process.Start(new ProcessStartInfo(executable, arguments)
                {
                    UseShellExecute = true
                });

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"卸载软件出错: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 打开软件安装目录
        /// </summary>
        public static bool OpenInstallLocation(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            try
            {
                Process.Start(new ProcessStartInfo("explorer.exe", path)
                {
                    UseShellExecute = true
                });
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"打开安装目录出错: {ex.Message}");
                return false;
            }
        }
    }
    #endregion

    #region 页面逻辑
    public sealed partial class AppMgmt : Page, INotifyPropertyChanged
    {
        private List<SoftwareInfo> _allSoftware = new List<SoftwareInfo>();
        private ObservableCollection<SoftwareInfo> _filteredSoftware = new ObservableCollection<SoftwareInfo>();
        private string _searchText = string.Empty;
        private bool _isLoading;

        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<SoftwareInfo> FilteredSoftware
        {
            get => _filteredSoftware;
            set
            {
                _filteredSoftware = value;
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

        public AppMgmt()
        {
            this.InitializeComponent();
            this.Loaded += AppMgmt_Loaded;
        }

        private void AppMgmt_Loaded(object sender, RoutedEventArgs e)
        {
            _ = LoadSoftwareListAsync();
        }

        private async Task LoadSoftwareListAsync()
        {
            try
            {
                IsLoading = true;
                LoadingIndicator.Visibility = Visibility.Visible;
                AppListScrollViewer.Visibility = Visibility.Collapsed;
                EmptyState.Visibility = Visibility.Collapsed;
                TotalCountText.Text = "加载中...";

                var softwareList = await Task.Run(() => SoftwareInfoHelper.GetInstalledSoftware());

                _allSoftware = softwareList;
                await UpdateFilteredListAsync(softwareList);

                TotalCountText.Text = $"共 {softwareList.Count} 个应用";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"加载软件列表出错: {ex.Message}");
                EmptyState.Visibility = Visibility.Visible;
                TotalCountText.Text = "加载失败";
            }
            finally
            {
                IsLoading = false;
                LoadingIndicator.Visibility = Visibility.Collapsed;

                if (FilteredSoftware.Count == 0)
                {
                    EmptyState.Visibility = Visibility.Visible;
                }
                else
                {
                    AppListScrollViewer.Visibility = Visibility.Visible;
                }
            }
        }

        private async Task UpdateFilteredListAsync(IEnumerable<SoftwareInfo> items)
        {
            FilteredSoftware.Clear();

            const int batchSize = 50;
            var batches = items
                .Where(FilterSoftware)
                .Batch(batchSize);

            foreach (var batch in batches)
            {
                foreach (var item in batch)
                {
                    FilteredSoftware.Add(item);
                }
                await Task.Delay(10);
            }
        }

        private bool FilterSoftware(SoftwareInfo software)
        {
            if (string.IsNullOrWhiteSpace(_searchText))
                return true;

            return (software.Name?.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                   (software.Publisher?.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                   (software.Version?.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ?? false);
        }

        private async void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _searchText = SearchBox.Text?.Trim() ?? string.Empty;

            if (_allSoftware == null || _allSoftware.Count == 0)
                return;

            var currentSearch = _searchText;
            await Task.Delay(200);

            if (currentSearch != _searchText)
                return;

            await UpdateFilteredListAsync(_allSoftware);

            var count = FilteredSoftware.Count;
            TotalCountText.Text = string.IsNullOrWhiteSpace(_searchText)
                ? $"共 {_allSoftware.Count} 个应用"
                : $"共 {_allSoftware.Count} 个应用，找到 {count} 个匹配项";

            EmptyState.Visibility = count == 0 ? Visibility.Visible : Visibility.Collapsed;
            AppListScrollViewer.Visibility = count > 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            await LoadSoftwareListAsync();
        }

        private async void UninstallButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string uninstallString)
            {
                var software = button.DataContext as SoftwareInfo;
                if (software == null) return;

                var dialog = new ContentDialog
                {
                    Title = "确认卸载",
                    Content = $"确定要卸载 {software.DisplayName} 吗？",
                    PrimaryButtonText = "卸载",
                    CloseButtonText = "取消",
                    XamlRoot = this.XamlRoot
                };

                var result = await dialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    bool success = SoftwareInfoHelper.UninstallSoftware(uninstallString);
                    if (!success)
                    {
                        await new ContentDialog
                        {
                            Title = "卸载失败",
                            Content = "无法启动卸载程序，可能需要手动卸载。",
                            CloseButtonText = "确定",
                            XamlRoot = this.XamlRoot
                        }.ShowAsync();
                    }
                    else
                    {
                        await LoadSoftwareListAsync();
                    }
                }
            }
        }

        private void ToolsLink_Click(object sender, RoutedEventArgs e)
        {
            if (Frame.CanGoBack)
            {
                Frame.GoBack();
            }
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
    #endregion

    #region 扩展方法
    public static class EnumerableExtensions
    {
        public static IEnumerable<IEnumerable<T>> Batch<T>(this IEnumerable<T> source, int batchSize)
        {
            using (var enumerator = source.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    yield return YieldBatchElements(enumerator, batchSize - 1);
                }
            }
        }

        private static IEnumerable<T> YieldBatchElements<T>(IEnumerator<T> source, int batchSize)
        {
            yield return source.Current;
            for (int i = 0; i < batchSize && source.MoveNext(); i++)
            {
                yield return source.Current;
            }
        }
    }
    #endregion
}
