using IntuneMonitor.Data;
using IntuneMonitor.Data.Entities;
using IntuneMonitor.Services;
using Microsoft.EntityFrameworkCore;

namespace IntuneMonitor.Workers;

public class TrialReminderWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TrialReminderWorker> _logger;

    public TrialReminderWorker(IServiceScopeFactory scopeFactory, ILogger<TrialReminderWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger       = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await CheckTrialsAsync();
            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }

    private async Task CheckTrialsAsync()
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db    = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var email = scope.ServiceProvider.GetRequiredService<EmailNotificationService>();

        var now       = DateTime.UtcNow;
        var threshold = now.AddDays(3);

        var tenants = await db.Tenants
            .Where(t =>
                t.SubscriptionStatus == SubscriptionStatus.Trial &&
                t.TrialEndsAt.HasValue &&
                t.TrialEndsAt.Value > now &&
                t.TrialEndsAt.Value <= threshold &&
                t.TrialReminderSentAt == null)
            .ToListAsync();

        foreach (var tenant in tenants)
        {
            var adminUser = await db.Users
                .FirstOrDefaultAsync(u => u.TenantId == tenant.Id && u.Role == UserRole.Admin);

            if (adminUser is null) continue;

            var daysLeft = Math.Max(1, (int)Math.Ceiling((tenant.TrialEndsAt!.Value - now).TotalDays));

            try
            {
                await email.SendTrialEndingAsync(adminUser.Email, daysLeft);
                tenant.TrialReminderSentAt = DateTime.UtcNow;
                await db.SaveChangesAsync();
                _logger.LogInformation("Trial reminder sent to {Email} ({Days} days left)", adminUser.Email, daysLeft);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send trial reminder for tenant {TenantId}", tenant.Id);
            }
        }
    }
}
