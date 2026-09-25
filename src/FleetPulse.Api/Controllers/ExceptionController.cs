using FleetPulse.Application.DTOs.Live;
using FleetPulse.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FleetPulse.Api.Controllers;

[ApiController]
[Route("api/exceptions")]
public class ExceptionController : ControllerBase
{
    private readonly IExceptionService _exceptions;

    public ExceptionController(IExceptionService exceptions)
    {
        _exceptions = exceptions;
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Dispatcher")]
    public async Task<ActionResult<IReadOnlyList<ExceptionAlertDto>>> GetLiveExceptions(CancellationToken cancellationToken = default)
        => Ok(await _exceptions.GetLiveExceptionsAsync(cancellationToken));

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin,Dispatcher")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
    {
        var deleted = await _exceptions.DeleteAsync(id, cancellationToken);
        if (!deleted)
            return NotFound(new { message = "Alert not found." });
        return NoContent();
    }
}