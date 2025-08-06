using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace SystemInfoViewer.Helpers
{
    /// <summary>
    /// 启动项信息数据模型类
    /// </summary>
    public class StartupAppInfo
    {
        /// <summary>
        /// 应用名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 发布者
        /// </summary>
        public string Publisher { get; set; } = string.Empty;

        /// <summary>
        /// 启动路径
        /// </summary>
        public string Path { get; set; } = string.Empty;

        /// <summary>
        /// 启动项位置（注册表路径或文件夹）
        /// </summary>
        public string Location { get; set; } = string.Empty;

        /// <summary>
        /// 是否启用
        /// </summary>
        public bool IsEnabled { get; set; }

        /// <summary>
        /// 启动项ID（用于标识）
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// 显示状态文本
        /// </summary>
        public string Status => IsEnabled ? "已启用" : "已禁用";
    }

    /// <summary>
    /// 启动项管理帮助类
    /// 提供获取、启用/禁用系统启动项的功能
    /// </summary>
    public static class StartupAppHelper
    {
        // 注册表中启动项的位置（包含当前用户和所有用户，32位和64位）
        private static readonly (RegistryHive Hive, string Path)[] RegistryStartupPaths = new[]
        {
            // 当前用户
            (RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Run"),
            (RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\RunOnce"),
            
            // 所有用户
            (RegistryHive.LocalMachine, @"Software\Microsoft\Windows\CurrentVersion\Run"),
            (RegistryHive.LocalMachine, @"Software\Microsoft\Windows\CurrentVersion\RunOnce"),
            
            // 32位应用（所有用户）
            (RegistryHive.LocalMachine, @"Software\Wow6432Node\Microsoft\Windows\CurrentVersion\Run"),
            (RegistryHive.LocalMachine, @"Software\Wow6432Node\Microsoft\Windows\CurrentVersion\RunOnce")
        };

        /// <summary>
        /// 获取系统中所有启动项
        /// </summary>
        public static List<StartupAppInfo> GetStartupApps()
        {
            var startupApps = new List<StartupAppInfo>();

            // 从注册表获取启动项
            GetRegistryStartupApps(startupApps);

            // 从启动文件夹获取启动项
            GetFolderStartupApps(startupApps);

            // 去重并按名称排序（根据路径去重，避免同一程序多次出现）
            return startupApps
                .GroupBy(a => a.Path.ToLowerInvariant())
                .Select(g => g.First())
                .Where(a => !string.IsNullOrEmpty(a.Name))
                .OrderBy(a => a.Name)
                .ToList();
        }

        /// <summary>
        /// 从注册表获取启动项
        /// </summary>
        private static void GetRegistryStartupApps(List<StartupAppInfo> startupApps)
        {
            foreach (var (hive, path) in RegistryStartupPaths)
            {
                try
                {
                    using (var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64))
                    using (var key = baseKey.OpenSubKey(path))
                    {
                        if (key == null) continue;

                        // 遍历注册表项中的所有值（每个值对应一个启动项）
                        foreach (var valueName in key.GetValueNames())
                        {
                            try
                            {
                                var value = key.GetValue(valueName) as string;
                                if (string.IsNullOrEmpty(value)) continue;

                                // 解析启动项路径（处理带引号的路径）
                                var parsedPath = ParseStartupPath(value);

                                // 创建启动项信息对象
                                startupApps.Add(new StartupAppInfo
                                {
                                    Id = $"Registry_{hive}_{path}_{valueName}", // 唯一标识（用于后续操作）
                                    Name = string.IsNullOrEmpty(valueName) ? Path.GetFileName(parsedPath) : valueName,
                                    Path = parsedPath,
                                    Publisher = GetPublisherFromPath(parsedPath),
                                    Location = $"注册表: {hive}\\{path}",
                                    IsEnabled = true // 注册表中存在即视为启用
                                });
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"解析注册表启动项 {valueName} 失败: {ex.Message}");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"访问注册表路径 {hive}\\{path} 失败: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 从启动文件夹获取启动项（快捷方式）
        /// </summary>
        private static void GetFolderStartupApps(List<StartupAppInfo> startupApps)
        {
            // 启动文件夹路径（当前用户和所有用户）
            var startupFolders = new[]
            {
                Environment.GetFolderPath(Environment.SpecialFolder.Startup), // 当前用户
                Environment.GetFolderPath(Environment.SpecialFolder.CommonStartup) // 所有用户
            };

            foreach (var folder in startupFolders)
            {
                try
                {
                    if (!Directory.Exists(folder)) continue;

                    // 获取文件夹中的所有.lnk快捷方式
                    var shortcutFiles = Directory.EnumerateFiles(folder, "*.lnk", SearchOption.TopDirectoryOnly);

                    foreach (var shortcutPath in shortcutFiles)
                    {
                        try
                        {
                            // 解析快捷方式指向的目标路径
                            var targetPath = ShortcutHelper.GetShortcutTarget(shortcutPath);
                            if (string.IsNullOrEmpty(targetPath) || !File.Exists(targetPath)) continue;

                            // 创建启动项信息对象
                            startupApps.Add(new StartupAppInfo
                            {
                                Id = $"Folder_{shortcutPath}", // 唯一标识
                                Name = Path.GetFileNameWithoutExtension(shortcutPath),
                                Path = targetPath,
                                Publisher = GetPublisherFromPath(targetPath),
                                Location = $"文件夹: {folder}",
                                IsEnabled = true // 文件夹中存在即视为启用
                            });
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"解析快捷方式 {shortcutPath} 失败: {ex.Message}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"访问启动文件夹 {folder} 失败: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 启用/禁用启动项
        /// </summary>
        /// <param name="appInfo">启动项信息</param>
        /// <param name="enable">是否启用</param>
        /// <returns>操作是否成功</returns>
        public static bool SetStartupAppState(StartupAppInfo appInfo, bool enable)
        {
            try
            {
                if (appInfo.Id.StartsWith("Registry_"))
                {
                    // 处理注册表启动项（重命名实现禁用，恢复名称实现启用）
                    return SetRegistryStartupState(appInfo, enable);
                }
                else if (appInfo.Id.StartsWith("Folder_"))
                {
                    // 处理文件夹启动项（移动到禁用文件夹实现禁用，移回原位置实现启用）
                    return SetFolderStartupState(appInfo, enable);
                }
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"修改启动项状态失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 启用/禁用注册表启动项
        /// </summary>
        private static bool SetRegistryStartupState(StartupAppInfo appInfo, bool enable)
        {
            // 解析ID中的注册表信息（格式：Registry_Hive_Path_ValueName）
            var idParts = appInfo.Id.Split(new[] { '_' }, 4);
            if (idParts.Length < 4) return false;

            var hive = (RegistryHive)Enum.Parse(typeof(RegistryHive), idParts[1]);
            var path = idParts[2];
            var valueName = idParts[3];
            var disabledName = $"_{valueName}_Disabled"; // 禁用时的名称

            using (var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64))
            using (var key = baseKey.OpenSubKey(path, writable: true))
            {
                if (key == null) return false;

                if (enable)
                {
                    // 启用：如果存在禁用的名称，恢复原名称
                    if (key.GetValue(disabledName) != null)
                    {
                        var value = key.GetValue(disabledName);
                        key.DeleteValue(disabledName);
                        key.SetValue(valueName, value);
                        return true;
                    }
                }
                else
                {
                    // 禁用：如果存在原名称，重命名为禁用名称
                    if (key.GetValue(valueName) != null)
                    {
                        var value = key.GetValue(valueName);
                        key.DeleteValue(valueName);
                        key.SetValue(disabledName, value);
                        return true;
                    }
                }
                return false;
            }
        }

        /// <summary>
        /// 启用/禁用文件夹启动项（快捷方式）
        /// </summary>
        private static bool SetFolderStartupState(StartupAppInfo appInfo, bool enable)
        {
            // 解析ID中的文件夹路径（格式：Folder_OriginalPath）
            var originalPath = appInfo.Id.Substring("Folder_".Length);
            var disabledFolder = Path.Combine(Path.GetDirectoryName(originalPath), "DisabledStartup");

            // 确保禁用文件夹存在
            if (!Directory.Exists(disabledFolder))
            {
                Directory.CreateDirectory(disabledFolder);
            }

            var disabledPath = Path.Combine(disabledFolder, Path.GetFileName(originalPath));

            if (enable)
            {
                // 启用：从禁用文件夹移回原位置
                if (File.Exists(disabledPath))
                {
                    File.Move(disabledPath, originalPath, overwrite: true);
                    return true;
                }
            }
            else
            {
                // 禁用：从原位置移到禁用文件夹
                if (File.Exists(originalPath))
                {
                    File.Move(originalPath, disabledPath, overwrite: true);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 解析启动项路径（处理带引号的路径）
        /// </summary>
        private static string ParseStartupPath(string rawPath)
        {
            if (rawPath.StartsWith("\"") && rawPath.Contains("\""))
            {
                // 提取引号内的路径（如 "C:\Program Files\App.exe" ）
                return rawPath.Substring(1, rawPath.IndexOf("\"", 1) - 1);
            }
            // 普通路径（如 C:\Windows\system32\app.exe ）
            return rawPath.Split(' ')[0];
        }

        /// <summary>
        /// 从程序路径获取发布者信息（通过文件版本信息）
        /// </summary>
        private static string GetPublisherFromPath(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    var versionInfo = FileVersionInfo.GetVersionInfo(filePath);
                    return !string.IsNullOrEmpty(versionInfo.CompanyName)
                        ? versionInfo.CompanyName
                        : "未知发布者";
                }
            }
            catch { }
            return "未知发布者";
        }
    }

    /// <summary>
    /// 快捷方式解析辅助类（用于获取.lnk文件指向的目标路径）
    /// </summary>
    public static class ShortcutHelper
    {
        [System.Runtime.InteropServices.DllImport("shell32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        private static extern int SHGetPathFromIDListW(IntPtr pidl, System.Text.StringBuilder pszPath);

        [System.Runtime.InteropServices.DllImport("shell32.dll")]
        private static extern IntPtr SHParseDisplayName(string name, IntPtr bindingContext, out IntPtr pidl, uint sfgaoIn, out uint sfgaoOut);

        /// <summary>
        /// 获取快捷方式指向的目标路径
        /// </summary>
        public static string GetShortcutTarget(string shortcutPath)
        {
            try
            {
                var sb = new System.Text.StringBuilder(260);
                var hr = SHParseDisplayName(shortcutPath, IntPtr.Zero, out var pidl, 0, out _);
                if (hr == 0)
                {
                    if (SHGetPathFromIDListW(pidl, sb) != 0)
                    {
                        return sb.ToString();
                    }
                }
            }
            catch { }
            return null;
        }
    }
}
