using IntuneMonitor.Models;

namespace IntuneMonitor.Services;

public class MockGraphService : IGraphService
{
    public Task<List<DeviceEnrollment>> GetRecentEnrollmentsAsync(int hoursBack = 24)
    {
        var devices = new List<DeviceEnrollment>
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
                    new() { AppId = "1", AppName = "Microsoft 365 Apps", IsInstalled = true, InstallState = "installed" },
                    new() { AppId = "2", AppName = "Company VPN",         IsInstalled = true, InstallState = "installed" },
                    new() { AppId = "3", AppName = "CrowdStrike Falcon",  IsInstalled = true, InstallState = "installed" },
                    new() { AppId = "4", AppName = "Cisco Webex",         IsInstalled = true, InstallState = "installed" },
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

        return Task.FromResult(devices);
    }
}
