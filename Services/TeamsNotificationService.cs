using System.Text;
using System.Text.Json;
using IntuneMonitor.Models;

namespace IntuneMonitor.Services;

public class TeamsNotificationService : INotificationService
{
    private readonly HttpClient _http;
    private readonly string? _webhookUrl;

    public TeamsNotificationService(HttpClient http, IConfiguration config)
    {
        _http = http;
        _webhookUrl = config["Notifications:TeamsWebhookUrl"];
    }

    public async Task SendDeviceReadyAsync(DeviceEnrollment device)
    {
        if (string.IsNullOrWhiteSpace(_webhookUrl))
        {
            Console.WriteLine("[Teams] No webhook URL configured — skipping notification.");
            return;
        }

        var timeToReady = device.TimeToReady.HasValue
            ? $"{(int)device.TimeToReady.Value.TotalMinutes} minutes"
            : "N/A";

        var payload = new
        {
            type = "message",
            attachments = new[]
            {
                new
                {
                    contentType = "application/vnd.microsoft.card.adaptive",
                    content = new
                    {
                        type = "AdaptiveCard",
                        version = "1.4",
                        body = new object[]
                        {
                            new
                            {
                                type = "TextBlock",
                                text = $"✅ Device Ready: {device.DeviceName}",
                                weight = "Bolder",
                                size = "Large",
                                color = "Good"
                            },
                            new
                            {
                                type = "FactSet",
                                facts = new[]
                                {
                                    new { title = "User",          value = device.UserPrincipalName },
                                    new { title = "Apps Installed", value = $"{device.InstalledApps} / {device.TotalRequiredApps}" },
                                    new { title = "Enrolled",       value = device.EnrolledDateTime.ToLocalTime().ToString("g") },
                                    new { title = "Time to Ready",  value = timeToReady }
                                }
                            }
                        }
                    }
                }
            }
        };

        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _http.PostAsync(_webhookUrl, content);

        if (!response.IsSuccessStatusCode)
            Console.WriteLine($"[Teams] Notification failed: {response.StatusCode}");
        else
            Console.WriteLine($"[Teams] Notified: {device.DeviceName} is ready.");
    }
}
