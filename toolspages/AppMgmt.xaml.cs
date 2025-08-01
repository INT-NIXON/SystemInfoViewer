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
            TotalCountText.Text = "������...";

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

                        TotalCountText.Text = $"�� {totalCount} ��Ӧ��";
                    });

                    System.Threading.Thread.Sleep(50);
                }

                this.DispatcherQueue.TryEnqueue(() =>
                {
                    LoadingIndicator.Visibility = Visibility.Collapsed;

                    if (_filteredSoftware?.Count == 0)
                    {
                        EmptyState.Visibility = Visibility.Visible;
                        TotalCountText.Text = "�� 0 ��Ӧ��";
                    }
                    else
                    {
                        SoftwareList.Visibility = Visibility.Visible;
                        TotalCountText.Text = $"�� {totalCount} ��Ӧ��";
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
                TotalCountText.Text = $"�� {totalCount} ��Ӧ��";
            }
            else
            {
                _filteredSoftware = _allSoftware.Where(s =>
                    s.Name.ToLower().Contains(searchText) ||
                    s.Publisher.ToLower().Contains(searchText) ||
                    s.Version.ToLower().Contains(searchText)
                ).ToList();

                SoftwareList.ItemsSource = _filteredSoftware;
                TotalCountText.Text = $"�� {totalCount} ��Ӧ�ã��ҵ� {_filteredSoftware.Count} ��ƥ����";
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
                    Title = "ȷ��ж��",
                    Content = $"ȷ��Ҫж�� {software.DisplayName} ��",
                    PrimaryButtonText = "ж��",
                    CloseButtonText = "ȡ��",
                    XamlRoot = this.XamlRoot
                };

                var result = await dialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    // ִ��ж�ز���
                    bool success = SoftwareInfoHelper.UninstallSoftware(uninstallString);
                    if (!success)
                    {
                        await new ContentDialog
                        {
                            Title = "ж��ʧ��",
                            Content = "�޷�����ж�س��򣬿�����Ҫ�ֶ�ж�ء�",
                            CloseButtonText = "ȷ��",
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
