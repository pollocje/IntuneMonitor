namespace IntuneMonitor.Data.Entities;

public class Tenant
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;

    // Their Azure AD tenant ID — used to scope Graph API calls
    public string MicrosoftTenantId { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public SubscriptionStatus SubscriptionStatus { get; set; } = SubscriptionStatus.Trial;

    public DateTime? TrialEndsAt { get; set; }

    public string? StripeCustomerId { get; set; }
    public string? StripeSubscriptionId { get; set; }

    public string? TeamsWebhookUrl { get; set; }
    public string? SlackWebhookUrl { get; set; }
    public string? NotificationEmail { get; set; }

    // Set during onboarding when we create the "Restart IME Service" remediation script in their tenant
    public string? RemediationScriptId { get; set; }

    // Navigation
    public List<AppUser> Users { get; set; } = new();
    public List<EnrollmentRecord> EnrollmentRecords { get; set; } = new();
}

public enum SubscriptionStatus
{
    Trial,
    Active,
    PastDue,
    Cancelled
}
