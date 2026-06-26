namespace IntuneMonitor.Models;

public class DeviceEnrollment
{
    public string DeviceId { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    public string UserPrincipalName { get; set; } = string.Empty;
    public DateTime EnrolledDateTime { get; set; }
    public DateTime? LastSyncDateTime { get; set; }
    public List<AppInstallStatus> AppStatuses { get; set; } = new();

    public int TotalRequiredApps => AppStatuses.Count;
    public int InstalledApps => AppStatuses.Count(a => a.IsInstalled);
    public bool IsFullyEnrolled => TotalRequiredApps > 0 && InstalledApps == TotalRequiredApps;
    public string StatusLabel => IsFullyEnrolled ? "Ready" : "Installing";
    public TimeSpan? TimeToReady => IsFullyEnrolled ? LastSyncDateTime - EnrolledDateTime : null;
}
