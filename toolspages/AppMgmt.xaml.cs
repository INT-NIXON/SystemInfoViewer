using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SystemInfoViewer.Helpers;

namespace SystemInfoViewer
{
    public sealed partial class AppMgmt : Page
    {
        private List<SoftwareInfo>? _allSoftware;
        private List<SoftwareInfo>? _filteredSoftware;

        public AppMgmt()
        {
            this.InitializeComponent();
            this.Loaded += AppMgmt_Loaded;
        }

        private void AppMgmt_Loaded(object sender, RoutedEventArgs e)
        {
            LoadSoftwareList();
        }

        private void LoadSoftwareList()
        {
            LoadingIndicator.Visibility = Visibility.Visible;
            SoftwareList.Visibility = Visibility.Collapsed;
            EmptyState.Visibility = Visibility.Collapsed;
            TotalCountText.Text = "加载中...";

            SoftwareList.ItemsSource = null;
            _allSoftware = null;
            _filteredSoftware = null;

            _ = Task.Run(() =>
            {
                var tempList = SoftwareInfoHelper.GetInstalledSoftware();
                var totalCount = tempList.Count;

                const int batchSize = 50;
                var totalBatches = (int)Math.Ceiling((double)tempList.Count / batchSize);

                _allSoftware = tempList;

                for (int i = 0; i < totalBatches; i++)
                {
                    var batch = tempList.Skip(i * batchSize).Take(batchSize).ToList();

                    this.DispatcherQueue.TryEnqueue(() =>
                    {
                        if (_filteredSoftware == null)
                        {
                            _filteredSoftware = new List<SoftwareInfo>(batch);
                        }
                        else
                        {
                            foreach (var item in batch)
                            {
                                _filteredSoftware.Add(item);
                            }
                        }

                        SoftwareList.ItemsSource = _filteredSoftware;

                        TotalCountText.Text = $"共 {totalCount} 个应用";
                    });

                    System.Threading.Thread.Sleep(50);
                }

                this.DispatcherQueue.TryEnqueue(() =>
                {
                    LoadingIndicator.Visibility = Visibility.Collapsed;

                    if (_filteredSoftware?.Count == 0)
                    {
                        EmptyState.Visibility = Visibility.Visible;
                        TotalCountText.Text = "共 0 个应用";
                    }
                    else
                    {
                        SoftwareList.Visibility = Visibility.Visible;
                        TotalCountText.Text = $"共 {totalCount} 个应用";
                    }
                });
            });
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_allSoftware == null) return;

            var totalCount = _allSoftware.Count;
            var searchText = SearchBox.Text?.Trim().ToLower() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(searchText))
            {
                _filteredSoftware = new List<SoftwareInfo>(_allSoftware);
                SoftwareList.ItemsSource = _filteredSoftware;
                TotalCountText.Text = $"共 {totalCount} 个应用";
            }
            else
            {
                _filteredSoftware = _allSoftware.Where(s =>
                    s.Name.ToLower().Contains(searchText) ||
                    s.Publisher.ToLower().Contains(searchText) ||
                    s.Version.ToLower().Contains(searchText)
                ).ToList();

                SoftwareList.ItemsSource = _filteredSoftware;
                TotalCountText.Text = $"共 {totalCount} 个应用，找到 {_filteredSoftware.Count} 个匹配项";
            }

            EmptyState.Visibility = (_filteredSoftware?.Count ?? 0) == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadSoftwareList();
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
                    // 执行卸载操作
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
                        LoadSoftwareList();
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
    }
}
