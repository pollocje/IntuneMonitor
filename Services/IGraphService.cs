using IntuneMonitor.Models;

namespace IntuneMonitor.Services;

public interface IGraphService
{
    Task<List<DeviceEnrollment>> GetRecentEnrollmentsAsync(int hoursBack = 24);

    // Triggers an immediate Intune sync on the device — wakes up IME to re-evaluate pending apps
    Task SyncDeviceAsync(string deviceId);

    // Triggers the "Restart IME Service" Proactive Remediation script on the device.
    // Requires the remediation script to have been created in the customer's tenant during onboarding.
    Task RestartImeServiceAsync(string deviceId, string scriptPolicyId);
}
