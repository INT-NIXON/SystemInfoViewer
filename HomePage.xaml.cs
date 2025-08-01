using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Management;
using System.Threading.Tasks;

namespace SystemInfoViewer
{
    public sealed partial class HomePage : Page
    {
        private DateTime _bootTime;
        private DispatcherTimer _timer;
        private Dictionary<string, object> _systemInfoCache = new Dictionary<string, object>();
        private bool _isFirstLoad = true;
        private bool _isPageLoaded = false;
        private bool _isDisposed = false;

        public HomePage()
        {
            this.InitializeComponent();
            this.Loaded += HomePage_Loaded;
            this.Unloaded += HomePage_Unloaded;
        }

        private void HomePage_Loaded(object sender, RoutedEventArgs e)
        {
            _isPageLoaded = true;
            InitializeSystemInfoAsync();
            InitializeTimer();
        }

        private void HomePage_Unloaded(object sender, RoutedEventArgs e)
        {
            _isPageLoaded = false;
            _isDisposed = true;

            if (_timer != null && _timer.IsEnabled)
            {
                _timer.Stop();
                _timer.Tick -= Timer_Tick;
                _timer = null;
            }
        }

        private void SetFontForElement(UIElement element)
        {
            if (element is TextBlock textBlock)
            {
                textBlock.FontFamily = (FontFamily)Application.Current.Resources["MiSansFont"];
            }
        }

        private async void InitializeSystemInfoAsync()
        {
            if (_isDisposed) return;

            try
            {
                ShowLoadingIndicators();

                await Task.Run(() =>
                {
                    if (_isDisposed) return;

                    try
                    {
                        GetSystemBasicInfo();

                        GetProcessorInfo();

                        GetMemoryInfo();

                        GetDiskInfo();

                        GetGpuInfo();

                        GetSystemBootTime();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"初始化系统信息失败: {ex.Message}");
                    }
                });

                this.DispatcherQueue.TryEnqueue(() =>
                {
                    if (!_isDisposed)
                    {
                        UpdateSystemInfoUI();
                        HideLoadingIndicators();

                        if (_isFirstLoad && !_isDisposed)
                        {
                            UpdateMemoryUsage();
                            _isFirstLoad = false;
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"初始化系统信息异步任务失败: {ex.Message}");
            }
        }

        private void ShowLoadingIndicators()
        {
            if (!_isDisposed && OSVersionText != null)
            {
                OSVersionText.Text = "加载中...";
                OSArchitectureText.Text = "加载中...";
                ComputerNameText.Text = "加载中...";
                UserNameText.Text = "加载中...";
                ProcessorText.Text = "加载中...";
                MemoryText.Text = "加载中...";
                DiskInfoText.Text = "加载中...";
                GpuInfoText.Text = "加载中...";
                MemoryUsageText.Text = "加载中...";
                BootTimeText.Text = "加载中...";
                CurrentTimeText.Text = "加载中...";
                UptimeText.Text = "加载中...";
            }
        }

        private void HideLoadingIndicators()
        {

        }

        private void GetSystemBasicInfo()
        {
            if (_isDisposed) return;

            if (!_systemInfoCache.ContainsKey("OSVersion"))
            {
                _systemInfoCache["OSVersion"] = $"{Environment.OSVersion.VersionString} (Build {Environment.OSVersion.Version.Build})";
                _systemInfoCache["OSArchitecture"] = Environment.Is64BitOperatingSystem ? "64位" : "32位";
                _systemInfoCache["ComputerName"] = Environment.MachineName;
                _systemInfoCache["UserName"] = Environment.UserName;
            }
        }

        private void GetProcessorInfo()
        {
            if (_isDisposed) return;

            if (!_systemInfoCache.ContainsKey("Processor"))
            {
                try
                {
                    using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Processor"))
                    {
                        foreach (ManagementObject obj in searcher.Get())
                        {
                            _systemInfoCache["Processor"] = $"{obj["Name"]} ({obj["NumberOfCores"]}核心 {obj["NumberOfLogicalProcessors"]}线程)";
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _systemInfoCache["Processor"] = "获取失败";
                    Debug.WriteLine($"获取处理器信息失败: {ex.Message}");
                }
            }
        }

        private void GetMemoryInfo()
        {
            if (_isDisposed) return;

            if (!_systemInfoCache.ContainsKey("TotalMemory"))
            {
                try
                {
                    using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_ComputerSystem"))
                    {
                        foreach (ManagementObject obj in searcher.Get())
                        {
                            var totalMemoryBytes = Convert.ToDouble(obj["TotalPhysicalMemory"]);
                            var totalMemoryGB = Math.Round(totalMemoryBytes / (1024 * 1024 * 1024), 2);
                            _systemInfoCache["TotalMemory"] = $"{totalMemoryGB} GB";
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _systemInfoCache["TotalMemory"] = "获取失败";
                    Debug.WriteLine($"获取内存信息失败: {ex.Message}");
                }
            }
        }

        private void GetDiskInfo()
        {
            if (_isDisposed) return;

            if (!_systemInfoCache.ContainsKey("DiskInfo"))
            {
                try
                {
                    var diskInfo = new System.Text.StringBuilder();
                    using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_DiskDrive WHERE MediaType='Fixed hard disk media'"))
                    {
                        foreach (ManagementObject disk in searcher.Get())
                        {
                            var sizeBytes = Convert.ToUInt64(disk["Size"]);
                            var sizeGB = Math.Round(sizeBytes / (1024.0 * 1024 * 1024), 1);
                            diskInfo.AppendLine($"{disk["Model"]} - {sizeGB} GB");
                        }
                    }

                    _systemInfoCache["DiskInfo"] = diskInfo.ToString();
                }
                catch (Exception ex)
                {
                    _systemInfoCache["DiskInfo"] = "获取失败";
                    Debug.WriteLine($"获取磁盘信息失败: {ex.Message}");
                }
            }
        }

        private void GetGpuInfo()
        {
            if (_isDisposed) return;

            if (!_systemInfoCache.ContainsKey("GpuInfo"))
            {
                try
                {
                    var gpuInfo = new System.Text.StringBuilder();
                    using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_VideoController"))
                    {
                        foreach (ManagementObject gpu in searcher.Get())
                        {
                            string name = gpu["Name"]?.ToString()?.Trim() ?? "未知显卡";

                            string vram = "未知";
                            if (gpu["AdapterRAM"] != null)
                            {
                                try
                                {
                                    ulong vramBytes = Convert.ToUInt64(gpu["AdapterRAM"]);
                                    double vramGB = Math.Round(vramBytes / (1024.0 * 1024 * 1024), 1);
                                    vram = $"{vramGB} GB";
                                }
                                catch { }
                            }

                            string driverVersion = gpu["DriverVersion"]?.ToString() ?? "未知";

                            gpuInfo.AppendLine($"{name} | 显存: {vram} | 驱动: {driverVersion}");
                        }
                    }

                    if (gpuInfo.Length == 0)
                    {
                        gpuInfo.AppendLine("未检测到显卡信息");
                    }

                    _systemInfoCache["GpuInfo"] = gpuInfo.ToString().Trim();

                    this.DispatcherQueue.TryEnqueue(() =>
                    {
                        if (!_isDisposed && GpuLabelText != null && GpuInfoText != null)
                        {
                            var margin = new Thickness(0, -25, 0, 0); 
                            GpuLabelText.Margin = margin;
                            GpuInfoText.Margin = margin;
                        }
                    });
                }
                catch (Exception ex)
                {
                    _systemInfoCache["GpuInfo"] = "获取显卡信息失败";
                    Debug.WriteLine($"获取显卡信息失败: {ex.Message}");
                }
            }
        }

        private void GetSystemBootTime()
        {
            if (_isDisposed) return;

            if (!_systemInfoCache.ContainsKey("BootTime"))
            {
                try
                {
                    using (var searcher = new ManagementObjectSearcher("SELECT LastBootUpTime FROM Win32_OperatingSystem"))
                    {
                        foreach (ManagementObject obj in searcher.Get())
                        {
                            _bootTime = ManagementDateTimeConverter.ToDateTime(obj["LastBootUpTime"].ToString());
                            _systemInfoCache["BootTime"] = _bootTime.ToString("yyyy-MM-dd HH:mm:ss");
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _systemInfoCache["BootTime"] = "获取失败";
                    Debug.WriteLine($"获取系统启动时间失败: {ex.Message}");
                }
            }
        }

        private void UpdateSystemInfoUI()
        {
            if (_isDisposed) return;

            if (OSVersionText != null)
                OSVersionText.Text = _systemInfoCache.ContainsKey("OSVersion") ? _systemInfoCache["OSVersion"].ToString() : "未知";

            if (OSArchitectureText != null)
                OSArchitectureText.Text = _systemInfoCache.ContainsKey("OSArchitecture") ? _systemInfoCache["OSArchitecture"].ToString() : "未知";

            if (ComputerNameText != null)
                ComputerNameText.Text = _systemInfoCache.ContainsKey("ComputerName") ? _systemInfoCache["ComputerName"].ToString() : "未知";

            if (UserNameText != null)
                UserNameText.Text = _systemInfoCache.ContainsKey("UserName") ? _systemInfoCache["UserName"].ToString() : "未知";

            if (ProcessorText != null)
                ProcessorText.Text = _systemInfoCache.ContainsKey("Processor") ? _systemInfoCache["Processor"].ToString() : "未知";

            if (MemoryText != null)
                MemoryText.Text = _systemInfoCache.ContainsKey("TotalMemory") ? _systemInfoCache["TotalMemory"].ToString() : "未知";

            if (DiskInfoText != null)
                DiskInfoText.Text = _systemInfoCache.ContainsKey("DiskInfo") ? _systemInfoCache["DiskInfo"].ToString() : "未知";

            if (GpuInfoText != null)
                GpuInfoText.Text = _systemInfoCache.ContainsKey("GpuInfo") ? _systemInfoCache["GpuInfo"].ToString() : "未知";

            if (BootTimeText != null)
                BootTimeText.Text = _systemInfoCache.ContainsKey("BootTime") ? _systemInfoCache["BootTime"].ToString() : "未知";
        }

        private void InitializeTimer()
        {
            if (_isDisposed) return;

            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += Timer_Tick;
            _timer.Start();
        }

        private void Timer_Tick(object sender, object e)
        {
            if (_isDisposed) return;

            var now = DateTime.Now;
            if (CurrentTimeText != null)
            {
                CurrentTimeText.Text = now.ToString("yyyy-MM-dd HH:mm:ss");
            }

            if (_bootTime != DateTime.MinValue && UptimeText != null)
            {
                var uptime = now - _bootTime;
                UptimeText.Text = $"{uptime.Days}天 {uptime.Hours}小时 {uptime.Minutes}分 {uptime.Seconds}秒";
            }

            UpdateMemoryUsage();
        }

        private async void UpdateMemoryUsage()
        {
            if (_isDisposed) return;

            try
            {
                await Task.Run(() =>
                {
                    if (_isDisposed) return;

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
                                var usagePercent = Math.Round((usedMemoryKB / (double)totalMemoryKB) * 100, 1);

                                this.DispatcherQueue.TryEnqueue(() =>
                                {
                                    if (!_isDisposed && MemoryUsageText != null)
                                    {
                                        MemoryUsageText.Text = $"{usedMemoryGB}GB / {totalMemoryGB}GB ({usagePercent}%)";
                                    }
                                });

                                break;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        this.DispatcherQueue.TryEnqueue(() =>
                        {
                            if (!_isDisposed && MemoryUsageText != null)
                            {
                                MemoryUsageText.Text = "获取失败";
                            }
                        });
                        Debug.WriteLine($"获取内存使用失败: {ex.Message}");
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"更新内存使用异步任务失败: {ex.Message}");
            }
        }
    }
}
