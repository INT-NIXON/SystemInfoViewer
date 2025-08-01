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
            // ҳ�����ʱ�Զ���������б�
            LoadSoftwareList();
        }

        private void LoadSoftwareList()
        {
            LoadingIndicator.Visibility = Visibility.Visible;
            SoftwareList.Visibility = Visibility.Collapsed;
            EmptyState.Visibility = Visibility.Collapsed;

            // ��������б�
            SoftwareList.ItemsSource = null;
            _allSoftware = null;
            _filteredSoftware = null;

            // ʹ��Task.Run�ں�̨�߳�ִ�к�ʱ����
            _ = Task.Run(() =>
            {
                // 1. �ں�̨�̻߳�ȡ�����������
                var tempList = SoftwareInfoHelper.GetInstalledSoftware();

                // 2. �����θ���UI������һ���Լ��ع������ݵ��±�����
                const int batchSize = 50; // ÿ�θ���50��
                var totalBatches = (int)Math.Ceiling((double)tempList.Count / batchSize);

                _allSoftware = tempList; // ���������б�

                for (int i = 0; i < totalBatches; i++)
                {
                    // ��ȡ��ǰ��������
                    var batch = tempList.Skip(i * batchSize).Take(batchSize).ToList();

                    // ȷ����UI�̸߳��£���ָ�����ȼ���ʹ��Ĭ��ֵ��
                    this.DispatcherQueue.TryEnqueue(() =>
                    {
                        // �״μ���ʱ��ʼ���б���������׷��
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

                        // �����б�����Դ
                        SoftwareList.ItemsSource = _filteredSoftware;
                    });

                    // ÿ�����ݼ��غ�����ӳ٣���UI�̴߳�Ϣʱ��
                    System.Threading.Thread.Sleep(50);
                }

                // 3. ȫ��������ɺ����UI״̬
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
                // ����Ϊ��ʱ��ʾȫ��
                _filteredSoftware = new List<SoftwareInfo>(_allSoftware);
                SoftwareList.ItemsSource = _filteredSoftware;
                return;
            }

            var searchText = SearchBox.Text?.Trim().ToLower() ?? string.Empty;
            if (!string.IsNullOrEmpty(searchText) && _allSoftware != null)
            {
                // ��������б������ơ������߻�汾��
                _filteredSoftware = _allSoftware.Where(s =>
                    s.Name.ToLower().Contains(searchText) ||
                    s.Publisher.ToLower().Contains(searchText) ||
                    s.Version.ToLower().Contains(searchText)
                ).ToList();

                SoftwareList.ItemsSource = _filteredSoftware;
            }

            // ���¿�״̬��ʾ
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

                // ��ʾȷ�϶Ի���
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
                }
            }
        }

        private void ToolsLink_Click(object sender, RoutedEventArgs e)
        {
            // ����������ҳ�棨�������Ӧ�õ����ṹ�޸ģ�
            if (Frame.CanGoBack)
            {
                Frame.GoBack();
            }
            else
            {
                // ��ѡ�����߼��������Ҫ
                // Frame.Navigate(typeof(ToolsPage));
            }
        }
    }
}
