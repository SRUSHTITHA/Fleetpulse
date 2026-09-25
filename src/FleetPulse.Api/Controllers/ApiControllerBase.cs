using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace FleetPulse.Api.Controllers;

public abstract class ApiControllerBase : ControllerBase
{
    protected Guid? CurrentUserId()
    {
        var value = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(value, out var id) ? id : null;
    }
}