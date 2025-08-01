using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Globalization;

namespace SystemInfoViewer.Helpers
{
    /// <summary>
    /// 软件信息数据模型类
    /// </summary>
    public class SoftwareInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string Publisher { get; set; } = string.Empty;
        public DateTime? InstallDate { get; set; }
        public string InstallLocation { get; set; } = string.Empty;
        public string UninstallString { get; set; } = string.Empty;

        // 用于XAML绑定的显示名称
        public string DisplayName => string.IsNullOrEmpty(Name) ? "未知应用" : Name;

        // 格式化的安装日期
        public string FormattedInstallDate => InstallDate.HasValue
            ? InstallDate.Value.ToString("yyyy-MM-dd")
            : "未知日期";

        // 判断是否有有效的卸载字符串
        public bool HasUninstallString => !string.IsNullOrEmpty(UninstallString);
    }

    /// <summary>
    /// 软件信息帮助类
    /// 提供获取、解析和操作系统中安装软件的功能
    /// </summary>
    public static class SoftwareInfoHelper
    {
        /// <summary>
        /// 获取系统中已安装的软件列表
        /// </summary>
        /// <returns>软件信息列表</returns>
        public static List<SoftwareInfo> GetInstalledSoftware()
        {
            var softwareList = new List<SoftwareInfo>();

            // 注册表中存储已安装软件信息的路径
            // 包含32位和64位应用程序的路径
            var registryPaths = new[]
            {
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
                @"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
            };

            foreach (var path in registryPaths)
            {
                try
                {
                    // 尝试打开注册表项
                    using (var key = Registry.LocalMachine.OpenSubKey(path))
                    {
                        if (key == null)
                        {
                            Debug.WriteLine($"无法打开注册表路径: {path}");
                            continue;
                        }

                        // 遍历所有子项
                        foreach (var subKeyName in key.GetSubKeyNames())
                        {
                            try
                            {
                                using (var subKey = key.OpenSubKey(subKeyName))
                                {
                                    if (subKey != null)
                                    {
                                        var software = GetSoftwareInfoFromRegistry(subKey);
                                        if (software != null)
                                        {
                                            softwareList.Add(software);
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"处理子键 {subKeyName} 时出错: {ex.Message}");
                                continue; // 继续处理其他子键
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"访问注册表路径 {path} 时出错: {ex.Message}");
                    continue; // 继续处理其他注册表路径
                }
            }

            // 去重并按名称排序
            return softwareList
                .GroupBy(s => s.Name)
                .Select(g => g.First())
                .Where(s => !string.IsNullOrEmpty(s.Name))
                .OrderBy(s => s.Name)
                .ToList();
        }

        /// <summary>
        /// 从注册表项获取软件信息
        /// </summary>
        private static SoftwareInfo? GetSoftwareInfoFromRegistry(RegistryKey subKey)
        {
            try
            {
                // 获取软件名称
                var name = subKey.GetValue("DisplayName") as string;

                // 跳过没有名称的条目
                if (string.IsNullOrEmpty(name))
                    return null;

                return new SoftwareInfo
                {
                    Name = name,
                    Version = subKey.GetValue("DisplayVersion") as string ?? string.Empty,
                    Publisher = subKey.GetValue("Publisher") as string ?? string.Empty,
                    InstallDate = ParseInstallDate(subKey.GetValue("InstallDate") as string),
                    InstallLocation = subKey.GetValue("InstallLocation") as string ?? string.Empty,
                    UninstallString = subKey.GetValue("UninstallString") as string ?? string.Empty
                };
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"获取软件信息出错: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 解析安装日期（注册表中的格式通常是yyyyMMdd）
        /// </summary>
        private static DateTime? ParseInstallDate(string? installDateString)
        {
            if (string.IsNullOrEmpty(installDateString))
                return null;

            // 清理输入（去除空格和特殊字符）
            var cleanDate = installDateString.Trim();

            try
            {
                // 尝试多种常见日期格式
                var formats = new[]
                {
                    "yyyyMMdd",       // 最常见的注册表格式（如20230518）
                    "yyyy-MM-dd",     // 带连字符的格式
                    "MM/dd/yyyy",     // 美国格式
                    "dd/MM/yyyy",     // 欧洲格式
                    "yyyyMMddHHmmss"  // 带时间的长格式
                };

                if (DateTime.TryParseExact(cleanDate, formats,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out DateTime date))
                {
                    return date;
                }

                // 如果上述格式都不匹配，尝试默认解析
                if (DateTime.TryParse(cleanDate, out date))
                {
                    return date;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"日期解析错误 ({installDateString}): {ex.Message}");
            }

            // 所有尝试都失败时，返回null而非抛出异常
            return null;
        }

        /// <summary>
        /// 卸载软件
        /// </summary>
        /// <param name="uninstallString">卸载命令</param>
        /// <returns>是否成功启动卸载程序</returns>
        public static bool UninstallSoftware(string uninstallString)
        {
            if (string.IsNullOrEmpty(uninstallString))
                return false;

            try
            {
                string executable = string.Empty;
                string arguments = string.Empty;

                // 处理带引号的卸载命令
                if (uninstallString.StartsWith("\""))
                {
                    int quoteIndex = uninstallString.IndexOf("\"", 1);
                    if (quoteIndex > 0)
                    {
                        executable = uninstallString.Substring(1, quoteIndex - 1);
                        arguments = uninstallString.Substring(quoteIndex + 1).Trim();
                    }
                }
                else
                {
                    // 简单分割可执行文件和参数
                    string[] parts = uninstallString.Split(new[] { ' ' }, 2);
                    executable = parts[0];
                    if (parts.Length > 1)
                        arguments = parts[1];
                }

                // 启动卸载程序
                Process.Start(new ProcessStartInfo(executable, arguments)
                {
                    UseShellExecute = true
                });

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"卸载软件出错: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 打开软件安装目录
        /// </summary>
        public static bool OpenInstallLocation(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            try
            {
                Process.Start(new ProcessStartInfo("explorer.exe", path)
                {
                    UseShellExecute = true
                });
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"打开安装目录出错: {ex.Message}");
                return false;
            }
        }
    }
}
