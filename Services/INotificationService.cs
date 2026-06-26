using IntuneMonitor.Models;

namespace IntuneMonitor.Services;

public interface INotificationService
{
    Task SendDeviceReadyAsync(DeviceEnrollment device);
}
