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

        public SettingsPage()
        {
            this.InitializeComponent();
            LoadAnimationSetting();
        }

        /// <summary>
        /// ���ش��ڶ�������
        /// </summary>
        private void LoadAnimationSetting()
        {
            try
            {
                // ��ȡ��������ã�Ĭ��Ϊ���ַ�����ʾ�״�����
                string savedValue = FileHelper.ReadIniValue("Settings", ANIMATION_SETTING_KEY, "");

                if (string.IsNullOrEmpty(savedValue))
                {
                    // �״�������ʹ��ϵͳ��ǰ���ò�����
                    bool systemValue = SystemParametersInfoHelper.GetAnimationEnabled();
                    FileHelper.WriteIniValue("Settings", ANIMATION_SETTING_KEY, systemValue.ToString().ToLower());

                    _isProcessingToggle = true;
                    AnimationToggleSwitch.IsOn = systemValue;
                }
                else
                {
                    // ʹ�ñ��������ֵ
                    bool isEnabled = bool.Parse(savedValue);

                    _isProcessingToggle = true;
                    AnimationToggleSwitch.IsOn = isEnabled;
                }

                _isProcessingToggle = false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"���ض�������ʧ��: {ex.Message}");
                AnimationToggleSwitch.IsOn = true; // Ĭ������
                _isProcessingToggle = false;
            }
        }

        /// <summary>
        /// ���ڶ�������״̬�仯����
        /// </summary>
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
                // ��������ʱ�ָ�����״̬
                _isProcessingToggle = true;
                AnimationToggleSwitch.IsOn = !AnimationToggleSwitch.IsOn;
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

        /// <summary>
        /// �������ð�ť����¼�
        /// </summary>
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

                    // ɾ�������ļ�
                    if (File.Exists(iniPath))
                    {
                        File.Delete(iniPath);
                    }

                    // ����Ӧ��
                    RestartApp();
                }
                catch (Exception ex)
                {
                    // ��ʾ������Ϣ
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

        /// <summary>
        /// ����Ӧ�ó���
        /// </summary>
        private void RestartApp()
        {
            // ��ȡ��ǰӦ�õ�·�����ļ���
            string currentExePath = Process.GetCurrentProcess().MainModule.FileName;

            // ������ʵ��
            Process.Start(new ProcessStartInfo
            {
                FileName = currentExePath,
                UseShellExecute = true
            });

            // �رյ�ǰʵ��
            Application.Current.Exit();
        }
    }
}
