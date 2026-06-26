using IntuneMonitor.Data.Entities;
using IntuneMonitor.Models;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace IntuneMonitor.Services;

public class EmailNotificationService
{
    private readonly string? _apiKey;
    private readonly string _fromEmail;
    private readonly string _appUrl;
    private readonly ILogger<EmailNotificationService> _logger;

    public EmailNotificationService(IConfiguration config, ILogger<EmailNotificationService> logger)
    {
        _apiKey    = config["SendGrid:ApiKey"];
        _fromEmail = config["SendGrid:FromEmail"] ?? "notifications@intuneMonitor.com";
        _appUrl    = (config["AppUrl"] ?? "https://localhost:5001").TrimEnd('/');
        _logger    = logger;
    }

    public async Task SendDeviceReadyAsync(DeviceEnrollment device, Tenant? tenant = null)
    {
        var recipient = tenant?.NotificationEmail;
        if (string.IsNullOrWhiteSpace(recipient) || string.IsNullOrWhiteSpace(_apiKey))
            return;

        var timeToReady = device.TimeToReady.HasValue
            ? $"{(int)device.TimeToReady.Value.TotalMinutes} minutes"
            : "N/A";

        var subject  = $"✅ {device.DeviceName} is ready";
        var dashLink = $"{_appUrl}/device/{device.DeviceId}";

        var plain = $"""
            {device.DeviceName} is ready.

            User:          {device.UserPrincipalName}
            Apps installed: {device.InstalledApps}/{device.TotalRequiredApps}
            Enrolled:      {device.EnrolledDateTime.ToLocalTime():g}
            Time to ready: {timeToReady}

            View device: {dashLink}

            — IntuneMonitor
            """;

        var html = $"""
            <!DOCTYPE html>
            <html>
            <body style="margin:0;padding:0;background:#f3f4f6;font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',sans-serif;">
              <table width="100%" cellpadding="0" cellspacing="0" style="background:#f3f4f6;padding:40px 0;">
                <tr><td align="center">
                  <table width="560" cellpadding="0" cellspacing="0" style="background:#fff;border-radius:12px;overflow:hidden;box-shadow:0 1px 4px rgba(0,0,0,.08);">

                    <tr>
                      <td style="background:#0066cc;padding:28px 36px;">
                        <span style="color:#fff;font-size:1.1rem;font-weight:700;">IntuneMonitor</span>
                      </td>
                    </tr>

                    <tr>
                      <td style="padding:36px 36px 20px;">
                        <p style="margin:0 0 6px;font-size:0.85rem;color:#16a34a;font-weight:700;text-transform:uppercase;letter-spacing:.05em;">Device Ready</p>
                        <h1 style="margin:0 0 24px;font-size:1.6rem;font-weight:700;color:#1a1a2e;">{device.DeviceName}</h1>

                        <table width="100%" cellpadding="0" cellspacing="0" style="border:1px solid #e5e7eb;border-radius:8px;overflow:hidden;">
                          <tr style="background:#f8fafc;">
                            <td style="padding:12px 16px;font-size:0.8rem;color:#888;font-weight:700;text-transform:uppercase;letter-spacing:.04em;width:40%;">User</td>
                            <td style="padding:12px 16px;font-size:0.9rem;color:#1a1a2e;">{device.UserPrincipalName}</td>
                          </tr>
                          <tr style="border-top:1px solid #e5e7eb;">
                            <td style="padding:12px 16px;font-size:0.8rem;color:#888;font-weight:700;text-transform:uppercase;letter-spacing:.04em;">Apps Installed</td>
                            <td style="padding:12px 16px;font-size:0.9rem;color:#1a1a2e;">{device.InstalledApps} / {device.TotalRequiredApps}</td>
                          </tr>
                          <tr style="border-top:1px solid #e5e7eb;background:#f8fafc;">
                            <td style="padding:12px 16px;font-size:0.8rem;color:#888;font-weight:700;text-transform:uppercase;letter-spacing:.04em;">Enrolled</td>
                            <td style="padding:12px 16px;font-size:0.9rem;color:#1a1a2e;">{device.EnrolledDateTime.ToLocalTime():g}</td>
                          </tr>
                          <tr style="border-top:1px solid #e5e7eb;">
                            <td style="padding:12px 16px;font-size:0.8rem;color:#888;font-weight:700;text-transform:uppercase;letter-spacing:.04em;">Time to Ready</td>
                            <td style="padding:12px 16px;font-size:0.9rem;color:#1a1a2e;font-weight:600;">{timeToReady}</td>
                          </tr>
                        </table>

                        <div style="margin-top:28px;">
                          <a href="{dashLink}" style="display:inline-block;background:#0066cc;color:#fff;text-decoration:none;padding:12px 24px;border-radius:8px;font-weight:600;font-size:0.95rem;">View Device →</a>
                        </div>
                      </td>
                    </tr>

                    <tr>
                      <td style="padding:20px 36px;border-top:1px solid #f3f4f6;">
                        <p style="margin:0;font-size:0.8rem;color:#aaa;">IntuneMonitor · You're receiving this because you enabled email notifications in your settings.</p>
                      </td>
                    </tr>

                  </table>
                </td></tr>
              </table>
            </body>
            </html>
            """;

        try
        {
            var client  = new SendGridClient(_apiKey);
            var from    = new EmailAddress(_fromEmail, "IntuneMonitor");
            var to      = new EmailAddress(recipient);
            var msg     = MailHelper.CreateSingleEmail(from, to, subject, plain, html);
            var response = await client.SendEmailAsync(msg);

            if ((int)response.StatusCode >= 400)
                _logger.LogWarning("SendGrid returned {Status} for {Recipient}", response.StatusCode, recipient);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send email notification to {Recipient}", recipient);
        }
    }
}
