using FleetPulse.Application.DTOs.Geo;
using FleetPulse.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FleetPulse.Api.Controllers;

[ApiController]
[Route("api/geo")]
[Authorize]
public class GeoController : ControllerBase
{
    private readonly IGeoService _geo;
    public GeoController(IGeoService geo)
    {
        _geo = geo;
    }

    [HttpGet("search")]
    [AllowAnonymous]
    public async Task<ActionResult<GeocodeResultDto>> Search([FromQuery] string q, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(q))
            return BadRequest(new { message = "Query is required." });

        var result = await _geo.GeocodeAsync(q, cancellationToken);
        if (result is null)
            return NotFound(new { message = "Location not found." });
        return Ok(result);
    }

    [HttpGet("route")]
    public async Task<ActionResult<RouteResultDto>> Route(
        [FromQuery] double originLat,
        [FromQuery] double originLng,
        [FromQuery] double destLat,
        [FromQuery] double destLng,
        CancellationToken cancellationToken)
    {
        var result = await _geo.GetRouteAsync(originLat, originLng, destLat, destLng, cancellationToken);
        return Ok(result);
    }
}
