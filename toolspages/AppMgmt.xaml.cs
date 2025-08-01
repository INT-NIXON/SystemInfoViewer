using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
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
            // 页面加载时自动加载软件列表
            LoadSoftwareList();
        }

        private void LoadSoftwareList()
        {
            LoadingIndicator.Visibility = Visibility.Visible;
            SoftwareList.Visibility = Visibility.Collapsed;
            EmptyState.Visibility = Visibility.Collapsed;

            // 清空现有列表
            SoftwareList.ItemsSource = null;
            _allSoftware = null;
            _filteredSoftware = null;

            // 使用Task.Run在后台线程执行耗时操作
            _ = Task.Run(() =>
            {
                // 1. 在后台线程获取所有软件数据
                var tempList = SoftwareInfoHelper.GetInstalledSoftware();

                // 2. 分批次更新UI（避免一次性加载过多数据导致崩溃）
                const int batchSize = 50; // 每次更新50条
                var totalBatches = (int)Math.Ceiling((double)tempList.Count / batchSize);

                _allSoftware = tempList; // 保存完整列表

                for (int i = 0; i < totalBatches; i++)
                {
                    // 截取当前批次数据
                    var batch = tempList.Skip(i * batchSize).Take(batchSize).ToList();

                    // 确保在UI线程更新（不指定优先级，使用默认值）
                    this.DispatcherQueue.TryEnqueue(() =>
                    {
                        // 首次加载时初始化列表，后续批次追加
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

                        // 更新列表数据源
                        SoftwareList.ItemsSource = _filteredSoftware;
                    });

                    // 每批数据加载后短暂延迟，给UI线程喘息时间
                    System.Threading.Thread.Sleep(50);
                }

                // 3. 全部加载完成后更新UI状态
                this.DispatcherQueue.TryEnqueue(() =>
                {
                    LoadingIndicator.Visibility = Visibility.Collapsed;

                    if (_filteredSoftware?.Count == 0)
                    {
                        EmptyState.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        SoftwareList.Visibility = Visibility.Visible;
                    }
                });
            });
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(SearchBox.Text) && _allSoftware != null)
            {
                // 搜索为空时显示全部
                _filteredSoftware = new List<SoftwareInfo>(_allSoftware);
                SoftwareList.ItemsSource = _filteredSoftware;
                return;
            }

            var searchText = SearchBox.Text?.Trim().ToLower() ?? string.Empty;
            if (!string.IsNullOrEmpty(searchText) && _allSoftware != null)
            {
                // 过滤软件列表（按名称、发布者或版本）
                _filteredSoftware = _allSoftware.Where(s =>
                    s.Name.ToLower().Contains(searchText) ||
                    s.Publisher.ToLower().Contains(searchText) ||
                    s.Version.ToLower().Contains(searchText)
                ).ToList();

                SoftwareList.ItemsSource = _filteredSoftware;
            }

            // 更新空状态显示
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

                // 显示确认对话框
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
                }
            }
        }

        private void ToolsLink_Click(object sender, RoutedEventArgs e)
        {
            // 导航到工具页面（根据你的应用导航结构修改）
            if (Frame.CanGoBack)
            {
                Frame.GoBack();
            }
            else
            {
                // 备选导航逻辑，如果需要
                // Frame.Navigate(typeof(ToolsPage));
            }
        }
    }
}
