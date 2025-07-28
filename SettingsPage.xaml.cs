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
        public SettingsPage()
        {
            this.InitializeComponent();
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