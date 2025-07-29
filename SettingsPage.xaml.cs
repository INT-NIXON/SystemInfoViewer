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
        /// 加载窗口动画设置
        /// </summary>
        private void LoadAnimationSetting()
        {
            try
            {
                string savedValue = FileHelper.ReadIniValue("Settings", ANIMATION_SETTING_KEY, "");

                if (string.IsNullOrEmpty(savedValue))
                {
                    bool systemValue = SystemParametersInfoHelper.GetAnimationEnabled();
                    FileHelper.WriteIniValue("Settings", ANIMATION_SETTING_KEY, systemValue.ToString().ToLower());

                    _isProcessingToggle = true;
                    AnimationToggleSwitch.IsOn = systemValue;
                }
                else
                {

                    bool isEnabled = bool.Parse(savedValue);

                    _isProcessingToggle = true;
                    AnimationToggleSwitch.IsOn = isEnabled;
                }

                _isProcessingToggle = false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"加载动画设置失败: {ex.Message}");
                AnimationToggleSwitch.IsOn = true;
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

                // 保存设置到配置文件
                FileHelper.WriteIniValue("Settings", ANIMATION_SETTING_KEY, isEnabled.ToString().ToLower());

                // 应用系统设置
                SystemParametersInfoHelper.SetAnimationEnabled(isEnabled);
            }
            catch (Exception ex)
            {
                // 发生错误时恢复开关状态
                _isProcessingToggle = true;
                AnimationToggleSwitch.IsOn = !AnimationToggleSwitch.IsOn;
                _isProcessingToggle = false;

                // 显示错误信息对话框
                ContentDialog errorDialog = new ContentDialog
                {
                    Title = "错误",
                    Content = $"更新窗口动画设置时出错: {ex.Message}",
                    CloseButtonText = "确定",
                    XamlRoot = this.XamlRoot
                };
                await errorDialog.ShowAsync();
            }
        }

        private async void DelConfig_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ContentDialog
            {
                Title = "确认重置",
                Content = "确定要重置所有设置吗？这将删除所有配置并重启应用。",
                PrimaryButtonText = "确定",
                CloseButtonText = "取消",
                XamlRoot = this.XamlRoot,
                RequestedTheme = this.ActualTheme
            };

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                try
                {
                    string iniPath = FileHelper.GetSetupIniPath();

                    // 删除配置文件
                    if (File.Exists(iniPath))
                    {
                        File.Delete(iniPath);
                    }

                    // 重启应用
                    RestartApp();
                }
                catch (Exception ex)
                {
                    // 显示错误信息
                    ContentDialog errorDialog = new ContentDialog
                    {
                        Title = "错误",
                        Content = $"重置设置时出错: {ex.Message}",
                        CloseButtonText = "确定",
                        XamlRoot = this.XamlRoot
                    };
                    await errorDialog.ShowAsync();
                }
            }
        }

        private void RestartApp()
        {
            // 获取当前应用的路径和文件名
            string currentExePath = Process.GetCurrentProcess().MainModule.FileName;

            // 启动新实例
            Process.Start(new ProcessStartInfo
            {
                FileName = currentExePath,
                UseShellExecute = true
            });

            // 关闭当前实例
            Application.Current.Exit();
        }
    }
}
