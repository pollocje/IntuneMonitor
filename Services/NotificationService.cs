using IntuneMonitor.Data.Entities;
using IntuneMonitor.Models;

namespace IntuneMonitor.Services;

public class NotificationService : INotificationService
{
    private readonly TeamsNotificationService _teams;
    private readonly EmailNotificationService _email;

    public NotificationService(TeamsNotificationService teams, EmailNotificationService email)
    {
        _teams = teams;
        _email = email;
    }

    public async Task SendDeviceReadyAsync(DeviceEnrollment device, Tenant? tenant = null)
    {
        await _teams.SendDeviceReadyAsync(device, tenant);
        await _email.SendDeviceReadyAsync(device, tenant);
    }
}
