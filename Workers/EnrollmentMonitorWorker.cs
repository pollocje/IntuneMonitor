using IntuneMonitor.Hubs;
using IntuneMonitor.Services;
using Microsoft.AspNetCore.SignalR;

namespace IntuneMonitor.Workers;

public class EnrollmentMonitorWorker : BackgroundService
{
    private readonly IGraphService _graphService;
    private readonly IHubContext<EnrollmentHub> _hub;
    private readonly INotificationService _notifications;
    private readonly HashSet<string> _notifiedDevices = new();

    public EnrollmentMonitorWorker(
        IGraphService graphService,
        IHubContext<EnrollmentHub> hub,
        INotificationService notifications)
    {
        _graphService = graphService;
        _hub = hub;
        _notifications = notifications;
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
                    await _notifications.SendDeviceReadyAsync(device);
            }

            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
    }
}
