using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Diagnostics;
using System.IO;
using static SystemInfoViewer.MainWindow;

namespace SystemInfoViewer
{
    public sealed partial class ToolsPage : Page
    {
        public ToolsPage()
        {
            this.InitializeComponent();
        }

        private void OpenTaskManager_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("taskmgr.exe");
        }

        private void Openregedit_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("regedit.exe");
        }

        private void Opencmd_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("cmd.exe");
        }

        private void Opencleanmgr_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("cleanmgr.exe");
        }

        private void Opengeek_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string appDirectory = AppDomain.CurrentDomain.BaseDirectory;
                string geekPath = Path.Combine(appDirectory, "Apps", "geek.exe");

                if (File.Exists(geekPath))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = geekPath,
                        UseShellExecute = true
                    });
                }
                else
                {
                    ShowErrorMessage("δ�ҵ�Geekж�ع��ߣ���ȷ��appsĿ¼����ȷ����");
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage($"����Geekʱ����: {ex.Message}");
            }
        }

        private void Openeverything_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string appDirectory = AppDomain.CurrentDomain.BaseDirectory;
                string everythingPath = Path.Combine(appDirectory, "Apps", "everything.exe");

                if (File.Exists(everythingPath))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = everythingPath,
                        UseShellExecute = true
                    });
                }
                else
                {
                    ShowErrorMessage("δ�ҵ�Everything�������ߣ���ȷ��appsĿ¼����ȷ����");
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage($"����Everythingʱ����: {ex.Message}");
            }
        }

        // ������Ӧ�ù���ҳ��
        private void OpenAppMgmtPage_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Instance.Navigate(typeof(AppMgmt));
        }

        private async void ShowErrorMessage(string message)
        {
            var dialog = new ContentDialog
            {
                Title = "����",
                Content = message,
                CloseButtonText = "ȷ��",
                XamlRoot = this.XamlRoot
            };

            await dialog.ShowAsync();
        }
    }
}
