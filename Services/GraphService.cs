using IntuneMonitor.Models;
using Microsoft.Graph;

namespace IntuneMonitor.Services;

public class GraphService : IGraphService
{
    private readonly GraphServiceClient _client;

    public GraphService(GraphServiceClient client)
    {
        _client = client;
    }

    public async Task<List<DeviceEnrollment>> GetRecentEnrollmentsAsync(int hoursBack = 24)
    {
        var cutoff = DateTime.UtcNow.AddHours(-hoursBack);

        var result = await _client.DeviceManagement.ManagedDevices.GetAsync(req =>
        {
            req.QueryParameters.Filter = $"enrolledDateTime ge {cutoff:yyyy-MM-ddTHH:mm:ssZ}";
            req.QueryParameters.Select = new[]
            {
                "id", "deviceName", "userPrincipalName",
                "enrolledDateTime", "lastSyncDateTime", "enrollmentState"
            };
        });

        var devices = new List<DeviceEnrollment>();

        foreach (var managed in result?.Value ?? new())
        {
            var appStatuses = await GetAppStatusesAsync(managed.Id!);

            devices.Add(new DeviceEnrollment
            {
                DeviceId          = managed.Id ?? string.Empty,
                DeviceName        = managed.DeviceName ?? "Unknown",
                UserPrincipalName = managed.UserPrincipalName ?? "Unknown",
                EnrolledDateTime  = managed.EnrolledDateTime?.UtcDateTime ?? DateTime.UtcNow,
                LastSyncDateTime  = managed.LastSyncDateTime?.UtcDateTime,
                AppStatuses       = appStatuses
            });
        }

        return devices;
    }

    public async Task SyncDeviceAsync(string deviceId)
    {
        await _client.DeviceManagement.ManagedDevices[deviceId].SyncDevice.PostAsync();
    }

    public async Task RestartImeServiceAsync(string deviceId, string scriptPolicyId)
    {
        await _client.DeviceManagement.ManagedDevices[deviceId]
            .InitiateOnDemandProactiveRemediation
            .PostAsync(new Microsoft.Graph.DeviceManagement.ManagedDevices.Item.InitiateOnDemandProactiveRemediation.InitiateOnDemandProactiveRemediationPostRequestBody
            {
                ScriptPolicyId = scriptPolicyId
            });
    }

    private async Task<List<AppInstallStatus>> GetAppStatusesAsync(string deviceId)
    {
        var apps = await _client.DeviceAppManagement.MobileApps.GetAsync();
        var statuses = new List<AppInstallStatus>();

        foreach (var app in apps?.Value ?? new())
        {
            var deviceStatuses = await _client.DeviceAppManagement
                .MobileApps[app.Id]
                .DeviceStatuses
                .GetAsync(req =>
                {
                    req.QueryParameters.Filter = $"deviceId eq '{deviceId}'";
                });

            var state = deviceStatuses?.Value?.FirstOrDefault();

            statuses.Add(new AppInstallStatus
            {
                AppId        = app.Id ?? string.Empty,
                AppName      = app.DisplayName ?? "Unknown App",
                IsInstalled  = state?.InstallState?.ToString() == "installed",
                InstallState = state?.InstallState?.ToString() ?? "unknown"
            });
        }

        return statuses;
    }
}
