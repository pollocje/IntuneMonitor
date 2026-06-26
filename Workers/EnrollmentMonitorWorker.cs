using IntuneMonitor.Data;
using IntuneMonitor.Data.Entities;
using IntuneMonitor.Hubs;
using IntuneMonitor.Models;
using IntuneMonitor.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace IntuneMonitor.Workers;

public class EnrollmentMonitorWorker : BackgroundService
{
    private readonly MockGraphService _mockGraph;
    private readonly GraphServiceFactory _graphFactory;
    private readonly IHubContext<EnrollmentHub> _hub;
    private readonly INotificationService _notifications;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EnrollmentMonitorWorker> _logger;

    private readonly Dictionary<Guid, HashSet<string>> _notifiedByTenant = new();
    private readonly HashSet<string> _mockNotified = new();

    public EnrollmentMonitorWorker(
        MockGraphService mockGraph,
        GraphServiceFactory graphFactory,
        IHubContext<EnrollmentHub> hub,
        INotificationService notifications,
        IServiceScopeFactory scopeFactory,
        ILogger<EnrollmentMonitorWorker> logger)
    {
        _mockGraph     = mockGraph;
        _graphFactory  = graphFactory;
        _hub           = hub;
        _notifications = notifications;
        _scopeFactory  = scopeFactory;
        _logger        = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var tenants = await GetConnectedTenantsAsync(stoppingToken);

                if (tenants.Count == 0 || !_graphFactory.IsConfigured)
                    await RunMockCycleAsync(stoppingToken);
                else
                    foreach (var tenant in tenants)
                        await PollTenantAsync(tenant, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error in enrollment monitor");
            }

            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
    }

    private async Task<List<Tenant>> GetConnectedTenantsAsync(CancellationToken ct)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            return await db.Tenants
                .Where(t => t.MicrosoftTenantId != null && t.MicrosoftTenantId != "")
                .ToListAsync(ct);
        }
        catch
        {
            return new List<Tenant>();
        }
    }

    // Dev/demo mode — no real tenants connected
    private async Task RunMockCycleAsync(CancellationToken ct)
    {
        var devices = await _mockGraph.GetRecentEnrollmentsAsync();
        await _hub.Clients.All.SendAsync("DevicesUpdated", devices, ct);

        foreach (var device in devices.Where(d => d.IsFullyEnrolled))
        {
            if (_mockNotified.Add(device.DeviceId))
                await _notifications.SendDeviceReadyAsync(device);
        }
    }

    private async Task PollTenantAsync(Tenant tenant, CancellationToken ct)
    {
        try
        {
            var svc     = _graphFactory.CreateForTenant(tenant.MicrosoftTenantId!);
            var devices = await svc.GetRecentEnrollmentsAsync();

            // Push only to clients belonging to this tenant's group
            await _hub.Clients.Group($"tenant-{tenant.Id}")
                .SendAsync("DevicesUpdated", devices, ct);

            if (!_notifiedByTenant.TryGetValue(tenant.Id, out var notified))
                _notifiedByTenant[tenant.Id] = notified = new HashSet<string>();

            foreach (var device in devices.Where(d => d.IsFullyEnrolled))
            {
                if (notified.Add(device.DeviceId))
                {
                    await _notifications.SendDeviceReadyAsync(device, tenant);
                    await TrySaveEnrollmentRecordAsync(tenant.Id, device);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to poll tenant {MicrosoftTenantId}", tenant.MicrosoftTenantId);
        }
    }

    private async Task TrySaveEnrollmentRecordAsync(Guid tenantId, DeviceEnrollment device)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var exists = await db.EnrollmentRecords
                .AnyAsync(r => r.DeviceId == device.DeviceId && r.ReadyAt.HasValue);
            if (exists) return;

            db.EnrollmentRecords.Add(new EnrollmentRecord
            {
                TenantId           = tenantId,
                DeviceId           = device.DeviceId,
                DeviceName         = device.DeviceName,
                UserPrincipalName  = device.UserPrincipalName,
                EnrolledAt         = device.EnrolledDateTime,
                ReadyAt            = DateTime.UtcNow,
                NotificationSentAt = DateTime.UtcNow,
                TotalApps          = device.TotalRequiredApps,
                InstalledApps      = device.InstalledApps
            });

            await db.SaveChangesAsync();
        }
        catch
        {
            // DB unavailable — silently skip
        }
    }
}
