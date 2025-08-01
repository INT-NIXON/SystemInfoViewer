using System;

namespace SystemInfoViewer.Helpers
{
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

        // 新增：判断是否有有效的卸载命令
        public bool HasUninstallString => !string.IsNullOrEmpty(UninstallString);
    }
}
