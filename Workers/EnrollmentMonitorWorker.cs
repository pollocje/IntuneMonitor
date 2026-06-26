using IntuneMonitor.Hubs;
using IntuneMonitor.Services;
using Microsoft.AspNetCore.SignalR;

namespace IntuneMonitor.Workers;

public class EnrollmentMonitorWorker : BackgroundService
{
    private readonly IGraphService _graphService;
    private readonly IHubContext<EnrollmentHub> _hub;
    private readonly HashSet<string> _notifiedDevices = new();

    public EnrollmentMonitorWorker(IGraphService graphService, IHubContext<EnrollmentHub> hub)
    {
        _graphService = graphService;
        _hub = hub;
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
                    Console.WriteLine($"[NOTIFY] {device.DeviceName} is ready — {device.UserPrincipalName}");
            }

            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
    }
}
