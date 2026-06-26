namespace IntuneMonitor.Data.Entities;

public class EnrollmentRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid TenantId { get; set; }

    public string DeviceId { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    public string UserPrincipalName { get; set; } = string.Empty;

    public DateTime EnrolledAt { get; set; }
    public DateTime? ReadyAt { get; set; }
    public DateTime? NotificationSentAt { get; set; }

    public int TotalApps { get; set; }
    public int InstalledApps { get; set; }

    // Serialized snapshot of app install states at completion
    public string? AppStatusesJson { get; set; }

    public bool IsComplete => ReadyAt.HasValue;
    public TimeSpan? TimeToReady => ReadyAt.HasValue ? ReadyAt.Value - EnrolledAt : null;

    // Navigation
    public Tenant Tenant { get; set; } = null!;
}
