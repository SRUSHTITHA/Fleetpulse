using FleetPulse.Application.DTOs.Location;
using FleetPulse.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FleetPulse.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LocationController : ApiControllerBase
{
    private readonly ILocationService _locationService;

    public LocationController(ILocationService locationService)
    {
        _locationService = locationService;
    }

    [HttpPost]
    [Authorize(Roles = "Driver")]
    public async Task<ActionResult<ReportLocationResponse>> Report(ReportLocationRequest request)
    {
        var userId = CurrentUserId();
        if (userId is null)
            return Unauthorized();

        var result = await _locationService.ReportLocationAsync(userId.Value, request);

        return result.Status switch
        {
            LocationReportStatus.Accepted => Ok(result),
            LocationReportStatus.Throttled => StatusCode(StatusCodes.Status429TooManyRequests, result),
            LocationReportStatus.TripNotFound => NotFound(result),
            _ => BadRequest(result)
        };
    }

    [HttpGet("trip/{tripId:guid}")]
    [Authorize(Roles = "Admin,Dispatcher")]
    public async Task<ActionResult<IReadOnlyList<LocationDto>>> History(Guid tripId, int take = 100)
    {
        return Ok(await _locationService.GetTripHistoryAsync(tripId, take));
    }
}