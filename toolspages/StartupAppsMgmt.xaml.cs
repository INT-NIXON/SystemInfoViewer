using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using SystemInfoViewer.Helpers;

namespace SystemInfoViewer.toolspages
{
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
            this.InitializeComponent();
            this.Loaded += StartupAppsMgmt_Loaded;
        }

        private void StartupAppsMgmt_Loaded(object sender, RoutedEventArgs e)
        {
            _ = LoadStartupAppsAsync();
        }

        private async Task LoadStartupAppsAsync()
        {
            try
            {
                IsLoading = true;
                LoadingIndicator.Visibility = Visibility.Visible;
                StartupListScrollViewer.Visibility = Visibility.Collapsed;
                EmptyState.Visibility = Visibility.Collapsed;
                TotalCountText.Text = "加载中...";

                // 在后台线程加载数据
                var startupApps = await Task.Run(() => StartupAppHelper.GetStartupApps());

                _allStartupApps = startupApps;

                // 分批更新UI
                await UpdateFilteredListAsync(startupApps);

                TotalCountText.Text = $"共 {startupApps.Count} 个启动项";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"加载启动项列表出错: {ex.Message}");
                EmptyState.Visibility = Visibility.Visible;
                TotalCountText.Text = "加载失败";
            }
            finally
            {
                IsLoading = false;
                LoadingIndicator.Visibility = Visibility.Collapsed;

                if (FilteredStartupApps.Count == 0)
                {
                    EmptyState.Visibility = Visibility.Visible;
                }
                else
                {
                    StartupListScrollViewer.Visibility = Visibility.Visible;
                }
            }
        }

        private async Task UpdateFilteredListAsync(IEnumerable<StartupAppInfo> items)
        {
            FilteredStartupApps.Clear();

            // 分批添加项目以减少UI卡顿
            const int batchSize = 50;
            var batches = items
                .Where(FilterStartupApp)
                .Batch(batchSize);

            foreach (var batch in batches)
            {
                foreach (var item in batch)
                {
                    FilteredStartupApps.Add(item);
                }

                // 给UI线程时间处理其他事件
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

        private async void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _searchText = SearchBox.Text?.Trim() ?? string.Empty;

            if (_allStartupApps == null || _allStartupApps.Count == 0)
                return;

            // 防抖处理 - 延迟执行搜索
            var currentSearch = _searchText;
            await Task.Delay(200); // 延迟

            // 确保搜索文本没有变化
            if (currentSearch != _searchText)
                return;

            await UpdateFilteredListAsync(_allStartupApps);

            var count = FilteredStartupApps.Count;
            TotalCountText.Text = string.IsNullOrWhiteSpace(_searchText)
                ? $"共 {_allStartupApps.Count} 个启动项"
                : $"共 {_allStartupApps.Count} 个启动项，找到 {count} 个匹配项";

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

                // 尝试更新启动项状态
                bool success = await Task.Run(() =>
                    StartupAppHelper.SetStartupAppState(appInfo, newState));

                if (!success)
                {
                    // 如果失败，恢复原来的状态
                    toggleSwitch.IsOn = !newState;

                    // 显示错误消息
                    await new ContentDialog
                    {
                        Title = "操作失败",
                        Content = $"无法{(newState ? "启用" : "禁用")}{appInfo.Name}，可能需要管理员权限。",
                        CloseButtonText = "确定",
                        XamlRoot = this.XamlRoot
                    }.ShowAsync();
                }
            }
        }

        private void ToolsLink_Click(object sender, RoutedEventArgs e)
        {
            NavigateBackToToolsPage();
        }

        private void NavigateBackToToolsPage()
        {
            MainWindow.NavigationService.Instance.Navigate(typeof(ToolsPage));
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}