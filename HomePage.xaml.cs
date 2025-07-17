using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Diagnostics;
using System.Management;

namespace SystemInfoViewer
{
    public sealed partial class HomePage : Page
    {
        private DateTime _bootTime;
        private DispatcherTimer _timer;

        public HomePage()
        {
            this.InitializeComponent();
            InitializeSystemInfo();
            InitializeTimer();
            // 强制设置数据文本的字体
            ApplyHarmonyFont();
        }
        private void ApplyHarmonyFont()
        {
            // 从应用资源中获取字体
            var harmonyFont = (FontFamily)Application.Current.Resources["HarmonyBoldFont"];

            // 设置所有数据显示的TextBlock字体
            OSVersionText.FontFamily = harmonyFont;
            OSArchitectureText.FontFamily = harmonyFont;
            ComputerNameText.FontFamily = harmonyFont;
            UserNameText.FontFamily = harmonyFont;
            ProcessorText.FontFamily = harmonyFont;
            MemoryText.FontFamily = harmonyFont;
            DiskInfoText.FontFamily = harmonyFont;
            MemoryUsageText.FontFamily = harmonyFont;
            BootTimeText.FontFamily = harmonyFont;
            CurrentTimeText.FontFamily = harmonyFont;
            UptimeText.FontFamily = harmonyFont;
        }
        private void InitializeSystemInfo()
        {
            // 获取系统启动时间
            using (var searcher = new ManagementObjectSearcher("SELECT LastBootUpTime FROM Win32_OperatingSystem"))
            {
                foreach (ManagementObject obj in searcher.Get())
                {
                    _bootTime = ManagementDateTimeConverter.ToDateTime(obj["LastBootUpTime"].ToString());
                    BootTimeText.Text = _bootTime.ToString("yyyy-MM-dd HH:mm:ss");
                    break;
                }
            }

            // 操作系统信息
            OSVersionText.Text = $"{Environment.OSVersion.VersionString} (Build {Environment.OSVersion.Version.Build})";
            OSArchitectureText.Text = Environment.Is64BitOperatingSystem ? "64位" : "32位";
            ComputerNameText.Text = Environment.MachineName;
            UserNameText.Text = Environment.UserName;

            // 处理器信息
            using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Processor"))
            {
                foreach (ManagementObject obj in searcher.Get())
                {
                    ProcessorText.Text = $"{obj["Name"]} ({obj["NumberOfCores"]}核心 {obj["NumberOfLogicalProcessors"]}线程)";
                    break;
                }
            }

            // 内存信息
            using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_ComputerSystem"))
            {
                foreach (ManagementObject obj in searcher.Get())
                {
                    var totalMemoryBytes = Convert.ToDouble(obj["TotalPhysicalMemory"]);
                    var totalMemoryGB = Math.Round(totalMemoryBytes / (1024 * 1024 * 1024), 2);
                    MemoryText.Text = $"{totalMemoryGB} GB";
                    break;
                }
            }

            // 磁盘信息
            var diskInfo = new System.Text.StringBuilder();
            int diskCount = 0;
            using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_DiskDrive WHERE MediaType='Fixed hard disk media'"))
            {
                foreach (ManagementObject disk in searcher.Get())
                {
                    diskCount++;
                    var sizeBytes = Convert.ToUInt64(disk["Size"]);
                    var sizeGB = Math.Round(sizeBytes / (1024.0 * 1024 * 1024), 1);
                    diskInfo.AppendLine($"磁盘: {disk["Model"]} - {sizeGB} GB");
                }
            }
            DiskInfoText.Text = diskInfo.ToString();
        }

        private void InitializeTimer()
        {
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += Timer_Tick;
            _timer.Start();
        }

        private void Timer_Tick(object sender, object e)
        {
            // 更新当前时间
            var now = DateTime.Now;
            CurrentTimeText.Text = now.ToString("yyyy-MM-dd HH:mm:ss");

            // 更新运行时间
            if (_bootTime != DateTime.MinValue)
            {
                var uptime = now - _bootTime;
                UptimeText.Text = $"{uptime.Days}天 {uptime.Hours}小时 {uptime.Minutes}分 {uptime.Seconds}秒";
            }

            // 更新内存使用情况
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT TotalVisibleMemorySize, FreePhysicalMemory FROM Win32_OperatingSystem"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        var totalMemoryKB = Convert.ToUInt64(obj["TotalVisibleMemorySize"]);
                        var freeMemoryKB = Convert.ToUInt64(obj["FreePhysicalMemory"]);
                        var usedMemoryKB = totalMemoryKB - freeMemoryKB;

                        var totalMemoryGB = Math.Round(totalMemoryKB / 1024.0 / 1024, 1);
                        var usedMemoryGB = Math.Round(usedMemoryKB / 1024.0 / 1024, 1);

                        MemoryUsageText.Text = $"{usedMemoryGB}GB / {totalMemoryGB}GB";
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                MemoryUsageText.Text = "获取失败";
                Debug.WriteLine($"获取内存使用失败: {ex.Message}");
            }
        }
    }
}