using FleetPulse.Application.DTOs.Live;
using FleetPulse.Application.Interfaces;
using FleetPulse.Api.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace FleetPulse.Api.Services;

public class LocationBroadcaster : ILocationBroadcaster
{
    private readonly IHubContext<LocationHub> _hub;

    public LocationBroadcaster(IHubContext<LocationHub> hub)
    {
        _hub = hub;
    }

    public Task BroadcastLocationAsync(LocationBroadcastDto payload)
        => _hub.Clients.Group(LocationHub.OpsGroup).SendAsync("BroadcastLocation", payload);

    public Task BroadcastExceptionAsync(ExceptionAlertDto payload)
        => _hub.Clients.Group(LocationHub.OpsGroup).SendAsync("SendAlert", payload);

    public Task BroadcastExceptionRemovedAsync(Guid exceptionId)
        => _hub.Clients.Group(LocationHub.OpsGroup).SendAsync("AlertRemoved", exceptionId);
}