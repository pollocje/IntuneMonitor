using Microsoft.AspNetCore.SignalR;

namespace IntuneMonitor.Hubs;

public class EnrollmentHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var tenantId = Context.GetHttpContext()?.Request.Query["tenantId"].ToString();
        if (!string.IsNullOrEmpty(tenantId))
            await Groups.AddToGroupAsync(Context.ConnectionId, $"tenant-{tenantId}");

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var tenantId = Context.GetHttpContext()?.Request.Query["tenantId"].ToString();
        if (!string.IsNullOrEmpty(tenantId))
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"tenant-{tenantId}");

        await base.OnDisconnectedAsync(exception);
    }
}
