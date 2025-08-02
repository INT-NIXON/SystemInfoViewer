using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using SystemInfoViewer.Helpers;

namespace SystemInfoViewer
{
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

                // 在后台线程加载数据
                var softwareList = await Task.Run(() => SoftwareInfoHelper.GetInstalledSoftware());

                _allSoftware = softwareList;

                // 分批更新UI
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

            // 分批添加项目以减少UI卡顿
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

                // 给UI线程时间处理其他事件
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

            // 防抖处理 - 延迟执行搜索
            var currentSearch = _searchText;
            await Task.Delay(200); // 延迟

            // 确保搜索文本没有变化
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

    // 扩展方法用于分批处理集合
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
}