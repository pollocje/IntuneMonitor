namespace IntuneMonitor.Data.Entities;

public class RefreshListItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid TenantId { get; set; }
    public string DeviceName { get; set; } = string.Empty;
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;

    public AppUser User { get; set; } = null!;
}
