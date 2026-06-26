using System.Text;
using System.Text.Json;
using IntuneMonitor.Data.Entities;
using IntuneMonitor.Models;

namespace IntuneMonitor.Services;

public class TeamsNotificationService
{
    private readonly HttpClient _http;

    public TeamsNotificationService(HttpClient http)
    {
        _http = http;
    }

    public async Task SendDeviceReadyAsync(DeviceEnrollment device, Tenant? tenant = null)
    {
        var webhookUrl = tenant?.TeamsWebhookUrl;
        if (string.IsNullOrWhiteSpace(webhookUrl))
            return;

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
        var response = await _http.PostAsync(webhookUrl, content);

        if (!response.IsSuccessStatusCode)
            Console.WriteLine($"[Teams] Notification failed: {response.StatusCode}");
    }
}
