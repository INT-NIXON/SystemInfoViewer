using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Diagnostics;

namespace SystemInfoViewer
{
    /// <summary>
    /// 提供应用程序特定行为以补充默认的 Application 类。
    /// </summary>
    public partial class App : Application
    {
        private Window? _window;

        // 添加静态属性用于访问主窗口
        public static MainWindow? MainWindow { get; private set; }

        /// <summary>
        /// 初始化单例应用程序对象。这是编写的代码的第一行执行，
        /// 因此是 main() 或 WinMain() 的逻辑等效项。
        /// </summary>
        public App()
        {
            this.InitializeComponent();

            // 注册全局异常处理
            this.UnhandledException += App_UnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        }

        // 明确指定使用 System 命名空间的 UnhandledExceptionEventArgs
        private void CurrentDomain_UnhandledException(object sender, System.UnhandledExceptionEventArgs e)
        {
            var exception = e.ExceptionObject as Exception;
            if (exception != null)
            {
                Debug.WriteLine($"应用程序域未处理异常: {exception.Message}\n{exception.StackTrace}");
                ShowErrorDialog(exception);
            }
        }

        // 明确指定使用 Microsoft.UI.Xaml 命名空间的 UnhandledExceptionEventArgs
        private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            Debug.WriteLine($"XAML未处理异常: {e.Message}\n{e.Exception.StackTrace}");
            ShowErrorDialog(e.Exception);
            e.Handled = true; // 标记为已处理，防止应用崩溃
        }

        private async void ShowErrorDialog(Exception ex)
        {
            if (MainWindow == null) return;

            var dialog = new ContentDialog
            {
                Title = "发生错误",
                Content = $"错误信息: {ex.Message}\n\n堆栈跟踪: {ex.StackTrace?.Substring(0, 500)}",
                CloseButtonText = "确定",
                XamlRoot = MainWindow.Content.XamlRoot
            };

            await dialog.ShowAsync();
        }

        /// <summary>
        /// 在应用程序启动时调用。
        /// </summary>
        /// <param name="args">有关启动请求和过程的详细信息。</param>
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            // 将窗口实例赋值给静态属性
            MainWindow = new MainWindow();
            _window = MainWindow;
            _window.Activate();
        }
    }
}