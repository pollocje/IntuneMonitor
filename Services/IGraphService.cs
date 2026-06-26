using IntuneMonitor.Models;

namespace IntuneMonitor.Services;

public interface IGraphService
{
    Task<List<DeviceEnrollment>> GetRecentEnrollmentsAsync(int hoursBack = 24);
}
