using FleetPulse.Application.DTOs.Trip;
using FleetPulse.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FleetPulse.Api.Controllers;

[ApiController]
[Route("api/trips")]
public class TripController : ApiControllerBase
{
    private readonly ITripService _tripService;
    public TripController(ITripService tripService)
    {
        _tripService = tripService;
    }

    [HttpPost("start")]
    [Authorize(Roles = "Driver")]
    public async Task<ActionResult<TripDto>> Start(StartTripRequest request)
    {
        var userId = CurrentUserId();
        if (userId is null)
            return Unauthorized();

        var result = await _tripService.StartTripAsync(userId.Value, request);
        if (result is null)
            return BadRequest(new { message = "Unable to start trip: no vehicle assigned or an in-progress trip already exists." });
        return Ok(result);
    }

    [HttpPost("end")]
    [Authorize(Roles = "Driver")]
    public async Task<ActionResult<TripDto>> End(TripIdRequest request)
    {
        var userId = CurrentUserId();
        if (userId is null)
            return Unauthorized();

        var result = await _tripService.EndTripAsync(userId.Value, request.TripId);
        if (result is null)
            return NotFound(new { message = "Trip not found or you are not its driver." });
        return Ok(result);
    }

    [HttpPost("cancel")]
    [Authorize(Roles = "Driver")]
    public async Task<ActionResult<TripDto>> Cancel(TripIdRequest request)
    {
        var userId = CurrentUserId();
        if (userId is null)
            return Unauthorized();

        var result = await _tripService.CancelTripAsync(userId.Value, request.TripId);
        if (result is null)
            return NotFound(new { message = "Trip not found or you are not its driver." });
        return Ok(result);
    }

    [HttpGet("active")]
    [Authorize(Roles = "Driver")]
    public async Task<ActionResult<TripDto>> Active()
    {
        var userId = CurrentUserId();
        if (userId is null)
            return Unauthorized();

        var result = await _tripService.GetActiveTripForDriverAsync(userId.Value);
        if (result is null)
            return NotFound(new { message = "No in-progress trip for this driver." });
        return Ok(result);
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Dispatcher")]
    public async Task<ActionResult<IReadOnlyList<TripDto>>> GetAll()
    {
        return Ok(await _tripService.GetDashboardTripsAsync());
    }
}