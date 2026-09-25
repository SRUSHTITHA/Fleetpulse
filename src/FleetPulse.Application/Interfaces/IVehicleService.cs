using FleetPulse.Application.DTOs.Vehicle;

namespace FleetPulse.Application.Interfaces;

public interface IVehicleService
{
    Task<IReadOnlyList<VehicleDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<VehicleDto?> CreateAsync(CreateVehicleRequest request, CancellationToken cancellationToken = default);
    Task<VehicleDto?> UpdateAsync(Guid vehicleId, UpdateVehicleRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DriverDto>> GetDriversAsync(CancellationToken cancellationToken = default);
    Task<VehicleDeleteResult> DeleteAsync(Guid vehicleId, CancellationToken cancellationToken = default);
}