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
            // ǿ�����������ı�������
            ApplyHarmonyFont();
        }
        private void ApplyHarmonyFont()
        {
            // ��Ӧ����Դ�л�ȡ����
            var harmonyFont = (FontFamily)Application.Current.Resources["HarmonyBoldFont"];

            // ��������������ʾ��TextBlock����
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
            // ��ȡϵͳ����ʱ��
            using (var searcher = new ManagementObjectSearcher("SELECT LastBootUpTime FROM Win32_OperatingSystem"))
            {
                foreach (ManagementObject obj in searcher.Get())
                {
                    _bootTime = ManagementDateTimeConverter.ToDateTime(obj["LastBootUpTime"].ToString());
                    BootTimeText.Text = _bootTime.ToString("yyyy-MM-dd HH:mm:ss");
                    break;
                }
            }

            // ����ϵͳ��Ϣ
            OSVersionText.Text = $"{Environment.OSVersion.VersionString} (Build {Environment.OSVersion.Version.Build})";
            OSArchitectureText.Text = Environment.Is64BitOperatingSystem ? "64λ" : "32λ";
            ComputerNameText.Text = Environment.MachineName;
            UserNameText.Text = Environment.UserName;

            // ��������Ϣ
            using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Processor"))
            {
                foreach (ManagementObject obj in searcher.Get())
                {
                    ProcessorText.Text = $"{obj["Name"]} ({obj["NumberOfCores"]}���� {obj["NumberOfLogicalProcessors"]}�߳�)";
                    break;
                }
            }

            // �ڴ���Ϣ
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

            // ������Ϣ
            var diskInfo = new System.Text.StringBuilder();
            int diskCount = 0;
            using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_DiskDrive WHERE MediaType='Fixed hard disk media'"))
            {
                foreach (ManagementObject disk in searcher.Get())
                {
                    diskCount++;
                    var sizeBytes = Convert.ToUInt64(disk["Size"]);
                    var sizeGB = Math.Round(sizeBytes / (1024.0 * 1024 * 1024), 1);
                    diskInfo.AppendLine($"����: {disk["Model"]} - {sizeGB} GB");
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
            // ���µ�ǰʱ��
            var now = DateTime.Now;
            CurrentTimeText.Text = now.ToString("yyyy-MM-dd HH:mm:ss");

            // ��������ʱ��
            if (_bootTime != DateTime.MinValue)
            {
                var uptime = now - _bootTime;
                UptimeText.Text = $"{uptime.Days}�� {uptime.Hours}Сʱ {uptime.Minutes}�� {uptime.Seconds}��";
            }

            // �����ڴ�ʹ�����
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
                MemoryUsageText.Text = "��ȡʧ��";
                Debug.WriteLine($"��ȡ�ڴ�ʹ��ʧ��: {ex.Message}");
            }
        }
    }
}