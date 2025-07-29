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
        // 默认值设置为true，与"显示窗口动画"的预期默认行为一致
        private const bool DEFAULT_ANIMATION_STATE = true;

        public SettingsPage()
        {
            this.InitializeComponent();
            LoadAnimationSetting();
        }

        /// <summary>
        /// 加载窗口动画设置，读取失败时默认打开
        /// </summary>
        private void LoadAnimationSetting()
        {
            try
            {
                // 读取保存的设置，默认为空字符串表示首次启动或无设置
                string savedValue = FileHelper.ReadIniValue("Settings", ANIMATION_SETTING_KEY, "");

                bool isEnabled;
                if (string.IsNullOrEmpty(savedValue))
                {
                    // 无保存的设置时使用默认值
                    isEnabled = DEFAULT_ANIMATION_STATE;
                    // 同时将默认值保存到配置文件
                    FileHelper.WriteIniValue("Settings", ANIMATION_SETTING_KEY, isEnabled.ToString().ToLower());
                }
                else
                {
                    // 尝试解析保存的设置值
                    if (!bool.TryParse(savedValue, out isEnabled))
                    {
                        // 解析失败时使用默认值
                        isEnabled = DEFAULT_ANIMATION_STATE;
                        FileHelper.WriteIniValue("Settings", ANIMATION_SETTING_KEY, isEnabled.ToString().ToLower());
                    }
                }

                // 应用最终确定的状态
                _isProcessingToggle = true;
                AnimationToggleSwitch.IsOn = isEnabled;
                _isProcessingToggle = false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"加载动画设置失败: {ex.Message}");
                // 任何异常情况下都使用默认打开状态
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

                // 保存设置到配置文件
                FileHelper.WriteIniValue("Settings", ANIMATION_SETTING_KEY, isEnabled.ToString().ToLower());

                // 应用系统设置
                SystemParametersInfoHelper.SetAnimationEnabled(isEnabled);
            }
            catch (Exception ex)
            {
                // 发生错误时恢复到默认打开状态
                _isProcessingToggle = true;
                AnimationToggleSwitch.IsOn = DEFAULT_ANIMATION_STATE;
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

                    if (File.Exists(iniPath))
                    {
                        File.Delete(iniPath);
                    }

                    // 重启应用后会使用默认打开状态
                    RestartApp();
                }
                catch (Exception ex)
                {
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
