namespace IntuneMonitor.Data.Entities;

public class AppUser
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid TenantId { get; set; }

    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;

    public UserRole Role { get; set; } = UserRole.Admin;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }

    public string? PasswordResetToken { get; set; }
    public DateTime? PasswordResetExpiry { get; set; }

    // Navigation
    public Tenant Tenant { get; set; } = null!;
}

public enum UserRole
{
    Admin,
    Viewer
}
