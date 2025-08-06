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
                TotalCountText.Text = "������...";

                // �ں�̨�̼߳�������
                var startupApps = await Task.Run(() => StartupAppHelper.GetStartupApps());

                _allStartupApps = startupApps;

                // ��������UI
                await UpdateFilteredListAsync(startupApps);

                TotalCountText.Text = $"�� {startupApps.Count} ��������";
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

            // ���������Ŀ�Լ���UI����
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

                // ��UI�߳�ʱ�䴦�������¼�
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

            // �������� - �ӳ�ִ������
            var currentSearch = _searchText;
            await Task.Delay(200); // �ӳ�

            // ȷ�������ı�û�б仯
            if (currentSearch != _searchText)
                return;

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

                // ���Ը���������״̬
                bool success = await Task.Run(() =>
                    StartupAppHelper.SetStartupAppState(appInfo, newState));

                if (!success)
                {
                    // ���ʧ�ܣ��ָ�ԭ����״̬
                    toggleSwitch.IsOn = !newState;

                    // ��ʾ������Ϣ
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