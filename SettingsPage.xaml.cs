using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SystemInfoViewer.Helpers;
using System.Diagnostics;
using System.IO;

namespace SystemInfoViewer
{
    public sealed partial class SettingsPage : Page
    {
        private const string ANIMATION_SETTING_KEY = "WindowAnimationEnabled";
        private bool _isProcessingToggle = false;
        // Ĭ��ֵ����Ϊtrue����"��ʾ���ڶ���"��Ԥ��Ĭ����Ϊһ��
        private const bool DEFAULT_ANIMATION_STATE = true;

        public SettingsPage()
        {
            this.InitializeComponent();
            LoadAnimationSetting();
        }

        /// <summary>
        /// ���ش��ڶ������ã���ȡʧ��ʱĬ�ϴ�
        /// </summary>
        private void LoadAnimationSetting()
        {
            try
            {
                // ��ȡ��������ã�Ĭ��Ϊ���ַ�����ʾ�״�������������
                string savedValue = FileHelper.ReadIniValue("Settings", ANIMATION_SETTING_KEY, "");

                bool isEnabled;
                if (string.IsNullOrEmpty(savedValue))
                {
                    // �ޱ��������ʱʹ��Ĭ��ֵ
                    isEnabled = DEFAULT_ANIMATION_STATE;
                    // ͬʱ��Ĭ��ֵ���浽�����ļ�
                    FileHelper.WriteIniValue("Settings", ANIMATION_SETTING_KEY, isEnabled.ToString().ToLower());
                }
                else
                {
                    // ���Խ������������ֵ
                    if (!bool.TryParse(savedValue, out isEnabled))
                    {
                        // ����ʧ��ʱʹ��Ĭ��ֵ
                        isEnabled = DEFAULT_ANIMATION_STATE;
                        FileHelper.WriteIniValue("Settings", ANIMATION_SETTING_KEY, isEnabled.ToString().ToLower());
                    }
                }

                // Ӧ������ȷ����״̬
                _isProcessingToggle = true;
                AnimationToggleSwitch.IsOn = isEnabled;
                _isProcessingToggle = false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"���ض�������ʧ��: {ex.Message}");
                // �κ��쳣����¶�ʹ��Ĭ�ϴ�״̬
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

                // �������õ������ļ�
                FileHelper.WriteIniValue("Settings", ANIMATION_SETTING_KEY, isEnabled.ToString().ToLower());

                // Ӧ��ϵͳ����
                SystemParametersInfoHelper.SetAnimationEnabled(isEnabled);
            }
            catch (Exception ex)
            {
                // ��������ʱ�ָ���Ĭ�ϴ�״̬
                _isProcessingToggle = true;
                AnimationToggleSwitch.IsOn = DEFAULT_ANIMATION_STATE;
                _isProcessingToggle = false;

                // ��ʾ������Ϣ�Ի���
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

                    // ����Ӧ�ú��ʹ��Ĭ�ϴ�״̬
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
