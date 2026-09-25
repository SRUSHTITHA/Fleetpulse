using FleetPulse.Application.DTOs.Vehicle;
using FleetPulse.Application.Interfaces;
using FleetPulse.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FleetPulse.Api.Controllers;

[ApiController]
[Route("api/vehicles")]
public class VehicleController : ControllerBase
{
    private readonly IVehicleService _vehicles;

    public VehicleController(IVehicleService vehicles)
    {
        _vehicles = vehicles;
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Dispatcher")]
    public async Task<ActionResult<IReadOnlyList<VehicleDto>>> GetAll(CancellationToken cancellationToken = default)
        => Ok(await _vehicles.GetAllAsync(cancellationToken));

    [HttpPost]
    [Authorize(Roles = "Admin,Dispatcher")]
    public async Task<ActionResult<VehicleDto>> Create(CreateVehicleRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.LicensePlate))
            return BadRequest(new { message = "Vehicle name and license plate are required." });
        if (request.DriverId is null)
            return BadRequest(new { message = "Assigning a driver is required." });
        if ((request.Latitude is null) != (request.Longitude is null))
            return BadRequest(new { message = "Both location coordinates are required." });

        var result = await _vehicles.CreateAsync(request, cancellationToken);
        if (result is null)
            return BadRequest(new { message = "Failed to create vehicle or the selected driver is unavailable." });
        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin,Dispatcher")]
    public async Task<ActionResult<VehicleDto>> Update(Guid id, UpdateVehicleRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.LicensePlate))
            return BadRequest(new { message = "Name and license plate are required." });
        if ((request.Latitude is null) != (request.Longitude is null))
            return BadRequest(new { message = "Both location coordinates are required." });

        var result = await _vehicles.UpdateAsync(id, request, cancellationToken);
        if (result is null)
            return NotFound(new { message = "Vehicle or assigned driver not found." });
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin,Dispatcher")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _vehicles.DeleteAsync(id, cancellationToken);
        return result switch
        {
            VehicleDeleteResult.NotFound => NotFound(new { message = "Vehicle not found." }),
            VehicleDeleteResult.Deactivated => Ok(new { message = "Vehicle has trip history and was deactivated instead.", deactivated = true }),
            _ => NoContent()
        };
    }

    [HttpGet("drivers")]
    [Authorize(Roles = "Admin,Dispatcher")]
    public async Task<ActionResult<IReadOnlyList<DriverDto>>> GetDrivers(CancellationToken cancellationToken = default)
        => Ok(await _vehicles.GetDriversAsync(cancellationToken));
}