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
        private const string HIDE_THEME_BUTTON_KEY = "HideThemeButton";
        private const string THEME_BUTTON_LEFT_ALIGN_KEY = "ThemeButtonLeftAlign";
        private const bool DEFAULT_ANIMATION_STATE = true;

        private bool _isProcessingToggle = false;
        private bool _isUpdatingFromCode = false;

        public SettingsPage()
        {
            InitializeComponent();
            LoadAnimationSetting();
            LoadThemeSetting();
            LoadHideThemeButtonSetting();
            LoadThemeButtonAlignmentSetting();

            var uiSettings = new UISettings();
            uiSettings.ColorValuesChanged += UiSettings_ColorValuesChanged;

            if (App.MainWindow != null)
            {
                App.MainWindow.ThemeChanged += MainWindow_ThemeChanged;
            }
        }

        #region �����л���ť��������
        private void LoadThemeButtonAlignmentSetting()
        {
            try
            {
                string savedValue = FileHelper.ReadIniValue("UI", THEME_BUTTON_LEFT_ALIGN_KEY, "false");
                bool isLeftAlign = bool.Parse(savedValue);
                ThemeButtonLeftAlignCheckBox.IsChecked = isLeftAlign;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"�������ⰴť��������ʧ��: {ex.Message}");
                ThemeButtonLeftAlignCheckBox.IsChecked = false;
            }
        }

        private void ThemeButtonLeftAlignCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            SaveThemeButtonAlignmentSetting(true);
        }

        private void ThemeButtonLeftAlignCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            SaveThemeButtonAlignmentSetting(false);
        }

        private void SaveThemeButtonAlignmentSetting(bool isLeftAlign)
        {
            try
            {
                FileHelper.WriteIniValue("UI", THEME_BUTTON_LEFT_ALIGN_KEY, isLeftAlign.ToString().ToLower());
                UpdateThemeButtonAlignment(isLeftAlign);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"�������ⰴť��������ʧ��: {ex.Message}");
            }
        }

        private void UpdateThemeButtonAlignment(bool isLeftAlign)
        {
            if (App.MainWindow != null && App.MainWindow.ThemeSwitchButton != null)
            {
                App.MainWindow.ThemeSwitchButton.HorizontalAlignment = isLeftAlign
                    ? HorizontalAlignment.Left
                    : HorizontalAlignment.Stretch;
            }
        }
        #endregion

        #region ��������
        private void LoadThemeSetting()
        {
            try
            {
                string savedTheme = FileHelper.ReadIniValue("Theme", THEME_SETTING_KEY, "system").ToLower();

                switch (savedTheme)
                {
                    case "light":
                        ThemeComboBox.SelectedIndex = 0;
                        break;
                    case "dark":
                        ThemeComboBox.SelectedIndex = 1;
                        break;
                    default:
                        ThemeComboBox.SelectedIndex = 2;
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"������������ʧ��: {ex.Message}");
                ThemeComboBox.SelectedIndex = 2;
            }
        }

        private void ThemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isUpdatingFromCode) return;

            if (ThemeComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                string theme = selectedItem.Tag.ToString().ToLower();

                FileHelper.WriteIniValue("Theme", THEME_SETTING_KEY, theme);

                UpdateAppTheme(theme);
            }
        }

        private void MainWindow_ThemeChanged(ElementTheme newTheme)
        {
            _isUpdatingFromCode = true;

            string savedTheme = FileHelper.ReadIniValue("Theme", THEME_SETTING_KEY, "system").ToLower();

            switch (savedTheme)
            {
                case "light":
                    ThemeComboBox.SelectedIndex = 0;
                    break;
                case "dark":
                    ThemeComboBox.SelectedIndex = 1;
                    break;
                default:
                    ThemeComboBox.SelectedIndex = 2;
                    break;
            }

            _isUpdatingFromCode = false;
        }

        private void UpdateAppTheme(string theme)
        {
            if (App.MainWindow != null)
            {
                ElementTheme targetTheme = theme switch
                {
                    "light" => ElementTheme.Light,
                    "dark" => ElementTheme.Dark,
                    _ => ElementTheme.Default
                };

                App.MainWindow.ApplyTheme(targetTheme);
            }
        }

        private void UiSettings_ColorValuesChanged(UISettings sender, object args)
        {
            string currentTheme = FileHelper.ReadIniValue("Theme", THEME_SETTING_KEY, "system").ToLower();
            if (currentTheme == "system")
            {
                UpdateAppTheme("system");
            }
        }
        #endregion

        #region ���ⰴť��ʾ����
        private void LoadHideThemeButtonSetting()
        {
            try
            {
                string savedValue = FileHelper.ReadIniValue("UI", HIDE_THEME_BUTTON_KEY, "false");
                bool isHidden = bool.Parse(savedValue);
                HideThemeButtonCheckBox.IsChecked = isHidden;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"�������ⰴť��ʾ����ʧ��: {ex.Message}");
                HideThemeButtonCheckBox.IsChecked = false;
            }
        }

        private void HideThemeButtonCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            SaveHideThemeButtonSetting(true);
        }

        private void HideThemeButtonCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            SaveHideThemeButtonSetting(false);
        }

        private void SaveHideThemeButtonSetting(bool isHidden)
        {
            try
            {
                FileHelper.WriteIniValue("UI", HIDE_THEME_BUTTON_KEY, isHidden.ToString().ToLower());
                UpdateThemeButtonVisibility(isHidden);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"�������ⰴť��ʾ����ʧ��: {ex.Message}");
            }
        }

        private void UpdateThemeButtonVisibility(bool isHidden)
        {
            if (App.MainWindow != null && App.MainWindow.ThemeSwitchButton != null)
            {
                App.MainWindow.ThemeSwitchButton.Visibility = isHidden ? Visibility.Collapsed : Visibility.Visible;
            }
        }
        #endregion

        #region ϵͳ��������
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
        #endregion

        #region ��������
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
        #endregion
    }
}
