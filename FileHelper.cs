using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.UI.Xaml;

namespace SystemInfoViewer.Helpers
{
    public static class FileHelper
    {
        // INI文件读写的Windows API
        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        private static extern long WritePrivateProfileString(string section, string key, string value, string filePath);

        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        private static extern int GetPrivateProfileString(string section, string key, string defaultValue, StringBuilder retVal, int size, string filePath);

        // 获取应用配置目录路径
        public static string GetAppConfigPath()
        {
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(appDataPath, ".SystemInfoViewer");
        }

        // 获取setup.ini文件路径
        public static string GetSetupIniPath()
        {
            return Path.Combine(GetAppConfigPath(), "setup.ini");
        }

        // 确保配置目录存在
        public static void EnsureConfigDirectoryExists()
        {
            string configPath = GetAppConfigPath();
            if (!Directory.Exists(configPath))
            {
                Directory.CreateDirectory(configPath);
            }
        }

        // 写入INI文件
        public static void WriteIniValue(string section, string key, string value)
        {
            EnsureConfigDirectoryExists();
            string iniPath = GetSetupIniPath();
            WritePrivateProfileString(section, key, value, iniPath);
        }

        // 读取INI文件
        public static string ReadIniValue(string section, string key, string defaultValue = "")
        {
            EnsureConfigDirectoryExists();
            string iniPath = GetSetupIniPath();

            // 读取缓冲区设置为255字符
            var retVal = new System.Text.StringBuilder(255);
            GetPrivateProfileString(section, key, defaultValue, retVal, 255, iniPath);
            return retVal.ToString();
        }
    }
}