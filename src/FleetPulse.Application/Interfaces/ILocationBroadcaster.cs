using FleetPulse.Application.DTOs.Live;

namespace FleetPulse.Application.Interfaces;

public interface ILocationBroadcaster
{
    Task BroadcastLocationAsync(LocationBroadcastDto payload);
    Task BroadcastExceptionAsync(ExceptionAlertDto payload);
    Task BroadcastExceptionRemovedAsync(Guid exceptionId);
}