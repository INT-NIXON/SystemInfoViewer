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
                        Debug.WriteLine($"��ʼ��ϵͳ��Ϣʧ��: {ex.Message}");
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
                Debug.WriteLine($"��ʼ��ϵͳ��Ϣ�첽����ʧ��: {ex.Message}");
            }
        }

        private void ShowLoadingIndicators()
        {
            if (!_isDisposed && OSVersionText != null)
            {
                OSVersionText.Text = "������...";
                OSArchitectureText.Text = "������...";
                ComputerNameText.Text = "������...";
                UserNameText.Text = "������...";
                ProcessorText.Text = "������...";
                MemoryText.Text = "������...";
                DiskInfoText.Text = "������...";
                GpuInfoText.Text = "������...";
                MemoryUsageText.Text = "������...";
                BootTimeText.Text = "������...";
                CurrentTimeText.Text = "������...";
                UptimeText.Text = "������...";
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
                _systemInfoCache["OSArchitecture"] = Environment.Is64BitOperatingSystem ? "64λ" : "32λ";
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
                            _systemInfoCache["Processor"] = $"{obj["Name"]} ({obj["NumberOfCores"]}���� {obj["NumberOfLogicalProcessors"]}�߳�)";
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _systemInfoCache["Processor"] = "��ȡʧ��";
                    Debug.WriteLine($"��ȡ��������Ϣʧ��: {ex.Message}");
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
                    _systemInfoCache["TotalMemory"] = "��ȡʧ��";
                    Debug.WriteLine($"��ȡ�ڴ���Ϣʧ��: {ex.Message}");
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
                    _systemInfoCache["DiskInfo"] = "��ȡʧ��";
                    Debug.WriteLine($"��ȡ������Ϣʧ��: {ex.Message}");
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
                            string name = gpu["Name"]?.ToString()?.Trim() ?? "δ֪�Կ�";

                            string vram = "δ֪";
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

                            string driverVersion = gpu["DriverVersion"]?.ToString() ?? "δ֪";

                            gpuInfo.AppendLine($"{name} | �Դ�: {vram} | ����: {driverVersion}");
                        }
                    }

                    if (gpuInfo.Length == 0)
                    {
                        gpuInfo.AppendLine("δ��⵽�Կ���Ϣ");
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
                    _systemInfoCache["GpuInfo"] = "��ȡ�Կ���Ϣʧ��";
                    Debug.WriteLine($"��ȡ�Կ���Ϣʧ��: {ex.Message}");
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
                    _systemInfoCache["BootTime"] = "��ȡʧ��";
                    Debug.WriteLine($"��ȡϵͳ����ʱ��ʧ��: {ex.Message}");
                }
            }
        }

        private void UpdateSystemInfoUI()
        {
            if (_isDisposed) return;

            if (OSVersionText != null)
                OSVersionText.Text = _systemInfoCache.ContainsKey("OSVersion") ? _systemInfoCache["OSVersion"].ToString() : "δ֪";

            if (OSArchitectureText != null)
                OSArchitectureText.Text = _systemInfoCache.ContainsKey("OSArchitecture") ? _systemInfoCache["OSArchitecture"].ToString() : "δ֪";

            if (ComputerNameText != null)
                ComputerNameText.Text = _systemInfoCache.ContainsKey("ComputerName") ? _systemInfoCache["ComputerName"].ToString() : "δ֪";

            if (UserNameText != null)
                UserNameText.Text = _systemInfoCache.ContainsKey("UserName") ? _systemInfoCache["UserName"].ToString() : "δ֪";

            if (ProcessorText != null)
                ProcessorText.Text = _systemInfoCache.ContainsKey("Processor") ? _systemInfoCache["Processor"].ToString() : "δ֪";

            if (MemoryText != null)
                MemoryText.Text = _systemInfoCache.ContainsKey("TotalMemory") ? _systemInfoCache["TotalMemory"].ToString() : "δ֪";

            if (DiskInfoText != null)
                DiskInfoText.Text = _systemInfoCache.ContainsKey("DiskInfo") ? _systemInfoCache["DiskInfo"].ToString() : "δ֪";

            if (GpuInfoText != null)
                GpuInfoText.Text = _systemInfoCache.ContainsKey("GpuInfo") ? _systemInfoCache["GpuInfo"].ToString() : "δ֪";

            if (BootTimeText != null)
                BootTimeText.Text = _systemInfoCache.ContainsKey("BootTime") ? _systemInfoCache["BootTime"].ToString() : "δ֪";
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
                UptimeText.Text = $"{uptime.Days}�� {uptime.Hours}Сʱ {uptime.Minutes}�� {uptime.Seconds}��";
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
                                MemoryUsageText.Text = "��ȡʧ��";
                            }
                        });
                        Debug.WriteLine($"��ȡ�ڴ�ʹ��ʧ��: {ex.Message}");
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"�����ڴ�ʹ���첽����ʧ��: {ex.Message}");
            }
        }
    }
}
