using IntuneMonitor.Data;
using IntuneMonitor.Data.Entities;
using IntuneMonitor.Hubs;
using IntuneMonitor.Services;
using Microsoft.AspNetCore.SignalR;

namespace IntuneMonitor.Workers;

public class EnrollmentMonitorWorker : BackgroundService
{
    private readonly IGraphService _graphService;
    private readonly IHubContext<EnrollmentHub> _hub;
    private readonly INotificationService _notifications;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly HashSet<string> _notifiedDevices = new();

    public EnrollmentMonitorWorker(
        IGraphService graphService,
        IHubContext<EnrollmentHub> hub,
        INotificationService notifications,
        IServiceScopeFactory scopeFactory)
    {
        _graphService = graphService;
        _hub = hub;
        _notifications = notifications;
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var devices = await _graphService.GetRecentEnrollmentsAsync();

            await _hub.Clients.All.SendAsync("DevicesUpdated", devices, stoppingToken);

            foreach (var device in devices.Where(d => d.IsFullyEnrolled))
            {
                if (_notifiedDevices.Add(device.DeviceId))
                {
                    await _notifications.SendDeviceReadyAsync(device);
                    await TrySaveEnrollmentRecordAsync(device);
                }
            }

            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
    }

    private async Task TrySaveEnrollmentRecordAsync(IntuneMonitor.Models.DeviceEnrollment device)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // Skip if this device is already recorded (handles restarts)
            var exists = await db.EnrollmentRecords
                .AnyAsync(r => r.DeviceId == device.DeviceId && r.ReadyAt.HasValue);
            if (exists) return;

            // In mock mode there's no real TenantId — find first tenant or skip
            var tenant = await db.Tenants.OrderBy(t => t.CreatedAt).FirstOrDefaultAsync();
            if (tenant is null) return;

            db.EnrollmentRecords.Add(new EnrollmentRecord
            {
                TenantId          = tenant.Id,
                DeviceId          = device.DeviceId,
                DeviceName        = device.DeviceName,
                UserPrincipalName = device.UserPrincipalName,
                EnrolledAt        = device.EnrolledDateTime,
                ReadyAt           = DateTime.UtcNow,
                NotificationSentAt = DateTime.UtcNow,
                TotalApps         = device.TotalRequiredApps,
                InstalledApps     = device.InstalledApps
            });

            await db.SaveChangesAsync();
        }
        catch
        {
            // DB unavailable (mock/dev mode) — silently skip
        }
    }
}
