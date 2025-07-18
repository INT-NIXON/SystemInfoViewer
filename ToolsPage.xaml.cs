using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;

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
                // ��ȡӦ�ó���װĿ¼
                string appDirectory = AppDomain.CurrentDomain.BaseDirectory;

                // ����geek.exe������·��
                string geekPath = Path.Combine(appDirectory, "Apps", "geek.exe");

                if (File.Exists(geekPath))
                {
                    // ʹ������·����������
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = geekPath,
                        UseShellExecute = true,  // ȷ��ʹ��shellִ��
                        Verb = "runas"          // ��ѡ���Թ���ԱȨ������
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
                // ��ȡӦ�ó���װĿ¼
                string appDirectory = AppDomain.CurrentDomain.BaseDirectory;

                // ����everything.exe������·��
                string everythingPath = Path.Combine(appDirectory, "Apps", "everything.exe");

                if (File.Exists(everythingPath))
                {
                    // ʹ������·����������
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