using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace FleetPulse.Api.Hubs;

[Authorize]
public class LocationHub : Hub
{
    public const string OpsGroup = "ops";

    public override async Task OnConnectedAsync()
    {
        var role = Context.User?.FindFirst(ClaimTypes.Role)?.Value;
        if (role is "Admin" or "Dispatcher")
            await Groups.AddToGroupAsync(Context.ConnectionId, OpsGroup);

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var role = Context.User?.FindFirst(ClaimTypes.Role)?.Value;
        if (role is "Admin" or "Dispatcher")
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, OpsGroup);

        await base.OnDisconnectedAsync(exception);
    }
}