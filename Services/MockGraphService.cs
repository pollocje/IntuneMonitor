using IntuneMonitor.Models;

namespace IntuneMonitor.Services;

public class MockGraphService : IGraphService
{
    private readonly List<DeviceEnrollment> _devices = CreateInitialDevices();
    private readonly Random _random = new();
    private int _callCount = 0;

    public Task<List<DeviceEnrollment>> GetRecentEnrollmentsAsync(int hoursBack = 24)
    {
        _callCount++;

        // Every other poll, install one pending app on a random installing device
        if (_callCount % 2 == 0)
        {
            var installing = _devices.Where(d => !d.IsFullyEnrolled).ToList();
            if (installing.Any())
            {
                var device = installing[_random.Next(installing.Count)];
                var nextApp = device.AppStatuses.FirstOrDefault(a => !a.IsInstalled);
                if (nextApp is not null)
                {
                    nextApp.IsInstalled = true;
                    nextApp.InstallState = "installed";
                    device.LastSyncDateTime = DateTime.UtcNow;
                }
            }
        }

        // Once all devices are ready, add a fresh one to keep the demo alive
        if (_devices.All(d => d.IsFullyEnrolled))
        {
            _devices.Add(new()
            {
                DeviceId = $"device-{_devices.Count + 1:000}",
                DeviceName = $"DESKTOP-NEW{_random.Next(100, 999)}",
                UserPrincipalName = "new.user@company.com",
                EnrolledDateTime = DateTime.UtcNow,
                LastSyncDateTime = DateTime.UtcNow,
                AppStatuses = new()
                {
                    new() { AppId = "1", AppName = "Microsoft 365 Apps", IsInstalled = false, InstallState = "notInstalled" },
                    new() { AppId = "2", AppName = "Company VPN",         IsInstalled = false, InstallState = "notInstalled" },
                    new() { AppId = "3", AppName = "CrowdStrike Falcon",  IsInstalled = false, InstallState = "notInstalled" },
                    new() { AppId = "4", AppName = "Cisco Webex",         IsInstalled = false, InstallState = "notInstalled" },
                }
            });
        }

        return Task.FromResult(_devices.ToList());
    }

    private static List<DeviceEnrollment> CreateInitialDevices() => new()
    {
        new()
        {
            DeviceId = "device-001",
            DeviceName = "DESKTOP-ABC123",
            UserPrincipalName = "john.smith@company.com",
            EnrolledDateTime = DateTime.UtcNow.AddHours(-2),
            LastSyncDateTime = DateTime.UtcNow.AddMinutes(-5),
            AppStatuses = new()
            {
                new() { AppId = "1", AppName = "Microsoft 365 Apps", IsInstalled = true,  InstallState = "installed" },
                new() { AppId = "2", AppName = "Company VPN",         IsInstalled = true,  InstallState = "installed" },
                new() { AppId = "3", AppName = "CrowdStrike Falcon",  IsInstalled = false, InstallState = "notInstalled" },
                new() { AppId = "4", AppName = "Cisco Webex",         IsInstalled = false, InstallState = "notInstalled" },
            }
        },
        new()
        {
            DeviceId = "device-002",
            DeviceName = "DESKTOP-XYZ789",
            UserPrincipalName = "sarah.jones@company.com",
            EnrolledDateTime = DateTime.UtcNow.AddHours(-1),
            LastSyncDateTime = DateTime.UtcNow.AddMinutes(-2),
            AppStatuses = new()
            {
                new() { AppId = "1", AppName = "Microsoft 365 Apps", IsInstalled = false, InstallState = "notInstalled" },
                new() { AppId = "2", AppName = "Company VPN",         IsInstalled = false, InstallState = "notInstalled" },
                new() { AppId = "3", AppName = "CrowdStrike Falcon",  IsInstalled = false, InstallState = "notInstalled" },
                new() { AppId = "4", AppName = "Cisco Webex",         IsInstalled = false, InstallState = "notInstalled" },
            }
        },
        new()
        {
            DeviceId = "device-003",
            DeviceName = "LAPTOP-DEF456",
            UserPrincipalName = "mike.taylor@company.com",
            EnrolledDateTime = DateTime.UtcNow.AddMinutes(-30),
            LastSyncDateTime = DateTime.UtcNow.AddMinutes(-1),
            AppStatuses = new()
            {
                new() { AppId = "1", AppName = "Microsoft 365 Apps", IsInstalled = false, InstallState = "notInstalled" },
                new() { AppId = "2", AppName = "Company VPN",         IsInstalled = false, InstallState = "notInstalled" },
                new() { AppId = "3", AppName = "CrowdStrike Falcon",  IsInstalled = false, InstallState = "notInstalled" },
                new() { AppId = "4", AppName = "Cisco Webex",         IsInstalled = false, InstallState = "notInstalled" },
            }
        }
    };
}
