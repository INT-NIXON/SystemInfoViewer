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
                // 获取应用程序安装目录
                string appDirectory = AppDomain.CurrentDomain.BaseDirectory;

                // 构建geek.exe的完整路径
                string geekPath = Path.Combine(appDirectory, "Apps", "geek.exe");

                if (File.Exists(geekPath))
                {
                    // 使用完整路径启动程序
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = geekPath,
                        UseShellExecute = true,  // 确保使用shell执行
                        Verb = "runas"          // 可选：以管理员权限运行
                    });
                }
                else
                {
                    ShowErrorMessage("未找到Geek卸载工具，请确保apps目录已正确部署。");
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage($"启动Geek时出错: {ex.Message}");
            }
        }

        private void Openeverything_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 获取应用程序安装目录
                string appDirectory = AppDomain.CurrentDomain.BaseDirectory;

                // 构建everything.exe的完整路径
                string everythingPath = Path.Combine(appDirectory, "Apps", "everything.exe");

                if (File.Exists(everythingPath))
                {
                    // 使用完整路径启动程序
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = everythingPath,
                        UseShellExecute = true
                    });
                }
                else
                {
                    ShowErrorMessage("未找到Everything搜索工具，请确保apps目录已正确部署。");
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage($"启动Everything时出错: {ex.Message}");
            }
        }

        private async void ShowErrorMessage(string message)
        {
            var dialog = new ContentDialog
            {
                Title = "错误",
                Content = message,
                CloseButtonText = "确定",
                XamlRoot = this.XamlRoot
            };

            await dialog.ShowAsync();
        }
    }
}