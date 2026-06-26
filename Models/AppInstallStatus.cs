namespace IntuneMonitor.Models;

public class AppInstallStatus
{
    public string AppId { get; set; } = string.Empty;
    public string AppName { get; set; } = string.Empty;
    public bool IsInstalled { get; set; }
    public string InstallState { get; set; } = string.Empty;
    public bool IsFailed => InstallState == "failed";
}
