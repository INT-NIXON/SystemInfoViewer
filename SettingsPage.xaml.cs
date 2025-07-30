using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Diagnostics;
using System.IO;
using SystemInfoViewer.Helpers;
using Windows.UI.ViewManagement;

namespace SystemInfoViewer
{
    public sealed partial class SettingsPage : Page
    {
        private const string ANIMATION_SETTING_KEY = "WindowAnimationEnabled";
        private const string THEME_SETTING_KEY = "CurrentTheme";
        private bool _isProcessingToggle = false;
        private bool _isUpdatingFromCode = false;
        private const bool DEFAULT_ANIMATION_STATE = true;

        public SettingsPage()
        {
            InitializeComponent();
            LoadAnimationSetting();
            LoadThemeSetting();

            // ����ϵͳ����仯
            var uiSettings = new UISettings();
            uiSettings.ColorValuesChanged += UiSettings_ColorValuesChanged;

            // ��������������仯��ͬ��������
            if (App.MainWindow != null)
            {
                App.MainWindow.ThemeChanged += MainWindow_ThemeChanged;
            }
        }

        // �����ü�������
        private void LoadThemeSetting()
        {
            try
            {
                // ��ȡ���ã�Ĭ��ֵΪsystem
                string savedTheme = FileHelper.ReadIniValue("Theme", THEME_SETTING_KEY, "system").ToLower();

                // ��������ѡ��������ѡ��
                switch (savedTheme)
                {
                    case "light":
                        ThemeComboBox.SelectedIndex = 0;
                        break;
                    case "dark":
                        ThemeComboBox.SelectedIndex = 1;
                        break;
                    default: // system������ֵ
                        ThemeComboBox.SelectedIndex = 2;
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"������������ʧ��: {ex.Message}");
                ThemeComboBox.SelectedIndex = 2; // Ĭ�ϸ���ϵͳ
            }
        }

        // ������ѡ��仯ʱ�������������
        private void ThemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isUpdatingFromCode) return;

            if (ThemeComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                string theme = selectedItem.Tag.ToString().ToLower();

                // ���浽����
                FileHelper.WriteIniValue("Theme", THEME_SETTING_KEY, theme);

                // ����Ӧ������
                UpdateAppTheme(theme);
            }
        }

        // ����������仯ʱͬ��������
        private void MainWindow_ThemeChanged(ElementTheme newTheme)
        {
            _isUpdatingFromCode = true;

            // ��ȡ��ǰ�������������
            string savedTheme = FileHelper.ReadIniValue("Theme", THEME_SETTING_KEY, "system").ToLower();

            // ���ݱ�����������͸���������
            switch (savedTheme)
            {
                case "light":
                    ThemeComboBox.SelectedIndex = 0;
                    break;
                case "dark":
                    ThemeComboBox.SelectedIndex = 1;
                    break;
                default: // system
                    ThemeComboBox.SelectedIndex = 2;
                    break;
            }

            _isUpdatingFromCode = false;
        }

        // ���������ַ�������Ӧ������
        private void UpdateAppTheme(string theme)
        {
            if (App.MainWindow != null)
            {
                ElementTheme targetTheme = theme switch
                {
                    "light" => ElementTheme.Light,
                    "dark" => ElementTheme.Dark,
                    _ => ElementTheme.Default // system��ӦDefault
                };

                // Ӧ������
                App.MainWindow.ApplyTheme(targetTheme);
            }
        }

        // ϵͳ����仯ʱ�����ڸ���ϵͳģʽ����Ч��
        private void UiSettings_ColorValuesChanged(UISettings sender, object args)
        {
            string currentTheme = FileHelper.ReadIniValue("Theme", THEME_SETTING_KEY, "system").ToLower();
            if (currentTheme == "system")
            {
                UpdateAppTheme("system");
            }
        }


        private void LoadAnimationSetting()
        {
            try
            {
                string savedValue = FileHelper.ReadIniValue("Settings", ANIMATION_SETTING_KEY, "");

                bool isEnabled;
                if (string.IsNullOrEmpty(savedValue))
                {
                    isEnabled = DEFAULT_ANIMATION_STATE;
                    FileHelper.WriteIniValue("Settings", ANIMATION_SETTING_KEY, isEnabled.ToString().ToLower());
                }
                else
                {
                    if (!bool.TryParse(savedValue, out isEnabled))
                    {
                        isEnabled = DEFAULT_ANIMATION_STATE;
                        FileHelper.WriteIniValue("Settings", ANIMATION_SETTING_KEY, isEnabled.ToString().ToLower());
                    }
                }
                _isProcessingToggle = true;
                AnimationToggleSwitch.IsOn = isEnabled;
                _isProcessingToggle = false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"���ض�������ʧ��: {ex.Message}");
                _isProcessingToggle = true;
                AnimationToggleSwitch.IsOn = DEFAULT_ANIMATION_STATE;
                _isProcessingToggle = false;
            }
        }

        private async void AnimationToggleSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            if (_isProcessingToggle)
                return;

            try
            {
                bool isEnabled = AnimationToggleSwitch.IsOn;

                FileHelper.WriteIniValue("Settings", ANIMATION_SETTING_KEY, isEnabled.ToString().ToLower());

                SystemParametersInfoHelper.SetAnimationEnabled(isEnabled);
            }
            catch (Exception ex)
            {
                _isProcessingToggle = true;
                AnimationToggleSwitch.IsOn = DEFAULT_ANIMATION_STATE;
                _isProcessingToggle = false;

                ContentDialog errorDialog = new ContentDialog
                {
                    Title = "����",
                    Content = $"���´��ڶ�������ʱ����: {ex.Message}",
                    CloseButtonText = "ȷ��",
                    XamlRoot = this.XamlRoot
                };
                await errorDialog.ShowAsync();
            }
        }

        private async void DelConfig_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ContentDialog
            {
                Title = "ȷ������",
                Content = "ȷ��Ҫ���������������⽫ɾ���������ò�����Ӧ�á�",
                PrimaryButtonText = "ȷ��",
                CloseButtonText = "ȡ��",
                XamlRoot = this.XamlRoot,
                RequestedTheme = this.ActualTheme
            };

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                try
                {
                    string iniPath = FileHelper.GetSetupIniPath();

                    if (File.Exists(iniPath))
                    {
                        File.Delete(iniPath);
                    }

                    RestartApp();
                }
                catch (Exception ex)
                {
                    ContentDialog errorDialog = new ContentDialog
                    {
                        Title = "����",
                        Content = $"��������ʱ����: {ex.Message}",
                        CloseButtonText = "ȷ��",
                        XamlRoot = this.XamlRoot
                    };
                    await errorDialog.ShowAsync();
                }
            }
        }

        private void RestartApp()
        {
            string currentExePath = Process.GetCurrentProcess().MainModule.FileName;

            Process.Start(new ProcessStartInfo
            {
                FileName = currentExePath,
                UseShellExecute = true
            });

            Application.Current.Exit();
        }
    }
}
