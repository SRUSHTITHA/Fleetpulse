using FleetPulse.Application.DTOs.Live;
using FleetPulse.Application.Interfaces;
using FleetPulse.Application.Interfaces.Repositories;
using FleetPulse.Domain.Entities;

namespace FleetPulse.Infrastructure.Services;

public class ExceptionService : IExceptionService
{
    private readonly IExceptionRepository _exceptions;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILocationBroadcaster _broadcaster;

    public ExceptionService(
        IExceptionRepository exceptions,
        IUnitOfWork unitOfWork,
        ILocationBroadcaster broadcaster)
    {
        _exceptions = exceptions;
        _unitOfWork = unitOfWork;
        _broadcaster = broadcaster;
    }

    public async Task<IReadOnlyList<ExceptionAlertDto>> GetLiveExceptionsAsync(CancellationToken cancellationToken = default)
    {
        var open = await _exceptions.GetActiveAsync(cancellationToken);
        return open.Select(ToAlert).ToList();
    }

    public async Task<bool> DeleteAsync(Guid exceptionId, CancellationToken cancellationToken = default)
    {
        var exception = await _exceptions.GetByIdAsync(exceptionId, cancellationToken);
        if (exception is null)
            return false;

        await _exceptions.DeleteAsync(exception, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Keep every operator's alert list in sync: the row is gone everywhere.
        await _broadcaster.BroadcastExceptionRemovedAsync(exceptionId);
        return true;
    }

    internal static ExceptionAlertDto ToAlert(FleetException e) => new()
    {
        Id = e.Id,
        TripId = e.TripId,
        DriverId = e.DriverId,
        DriverName = e.Trip?.Driver?.User?.FullName ?? string.Empty,
        VehicleName = e.Trip?.Vehicle?.Name ?? string.Empty,
        LicensePlate = e.Trip?.Vehicle?.LicensePlate ?? string.Empty,
        Origin = e.Trip?.Origin ?? string.Empty,
        Destination = e.Trip?.Destination ?? string.Empty,
        RuleType = e.RuleType,
        Severity = e.Severity,
        Status = e.Status,
        Title = e.Title,
        Description = e.Description,
        Latitude = e.Latitude,
        Longitude = e.Longitude,
        DetectedAt = e.DetectedAt
    };
}