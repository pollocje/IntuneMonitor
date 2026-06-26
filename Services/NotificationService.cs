using IntuneMonitor.Data.Entities;
using IntuneMonitor.Models;

namespace IntuneMonitor.Services;

public class NotificationService : INotificationService
{
    private readonly TeamsNotificationService _teams;
    private readonly SlackNotificationService _slack;
    private readonly EmailNotificationService _email;

    public NotificationService(
        TeamsNotificationService teams,
        SlackNotificationService slack,
        EmailNotificationService email)
    {
        _teams = teams;
        _slack = slack;
        _email = email;
    }

    public async Task SendDeviceReadyAsync(DeviceEnrollment device, Tenant? tenant = null)
    {
        await _teams.SendDeviceReadyAsync(device, tenant);
        await _slack.SendDeviceReadyAsync(device, tenant);
        await _email.SendDeviceReadyAsync(device, tenant);
    }
}
