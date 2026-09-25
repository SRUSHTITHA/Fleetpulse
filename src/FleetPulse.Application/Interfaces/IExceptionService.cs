using FleetPulse.Application.DTOs.Live;

namespace FleetPulse.Application.Interfaces;

public interface IExceptionService
{
    Task<IReadOnlyList<ExceptionAlertDto>> GetLiveExceptionsAsync(CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid exceptionId, CancellationToken cancellationToken = default);
}