using System.Text;
using System.Text.Json;
using IntuneMonitor.Data.Entities;
using IntuneMonitor.Models;

namespace IntuneMonitor.Services;

public class SlackNotificationService
{
    private readonly HttpClient _http;
    private readonly string _appUrl;

    public SlackNotificationService(HttpClient http, IConfiguration config)
    {
        _http   = http;
        _appUrl = (config["AppUrl"] ?? "https://localhost:5001").TrimEnd('/');
    }

    public async Task SendDeviceReadyAsync(DeviceEnrollment device, Tenant? tenant = null)
    {
        var webhookUrl = tenant?.SlackWebhookUrl;
        if (string.IsNullOrWhiteSpace(webhookUrl))
            return;

        var timeToReady = device.TimeToReady.HasValue
            ? $"{(int)device.TimeToReady.Value.TotalMinutes} minutes"
            : "N/A";

        var payload = new
        {
            text   = $"✅ {device.DeviceName} is ready",
            blocks = new object[]
            {
                new
                {
                    type = "header",
                    text = new { type = "plain_text", text = $"✅ Device Ready: {device.DeviceName}", emoji = true }
                },
                new
                {
                    type   = "section",
                    fields = new[]
                    {
                        new { type = "mrkdwn", text = $"*User*\n{device.UserPrincipalName}" },
                        new { type = "mrkdwn", text = $"*Apps Installed*\n{device.InstalledApps} / {device.TotalRequiredApps}" },
                        new { type = "mrkdwn", text = $"*Enrolled*\n{device.EnrolledDateTime.ToLocalTime():g}" },
                        new { type = "mrkdwn", text = $"*Time to Ready*\n{timeToReady}" }
                    }
                },
                new
                {
                    type     = "actions",
                    elements = new[]
                    {
                        new
                        {
                            type  = "button",
                            text  = new { type = "plain_text", text = "View Device →" },
                            url   = $"{_appUrl}/device/{device.DeviceId}",
                            style = "primary"
                        }
                    }
                }
            }
        };

        var json    = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _http.PostAsync(webhookUrl, content);

        if (!response.IsSuccessStatusCode)
            Console.WriteLine($"[Slack] Notification failed: {response.StatusCode}");
    }
}
