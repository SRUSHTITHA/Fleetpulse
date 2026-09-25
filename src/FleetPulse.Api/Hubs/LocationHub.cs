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
        if (IsOperator)
            await Groups.AddToGroupAsync(Context.ConnectionId, OpsGroup);

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (IsOperator)
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, OpsGroup);

        await base.OnDisconnectedAsync(exception);
    }

    private bool IsOperator
        => Context.User?.FindFirst(ClaimTypes.Role)?.Value is "Admin" or "Dispatcher";
}