using FleetPulse.Application.DTOs.ExceptionRule;
using FleetPulse.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FleetPulse.Api.Controllers;

[ApiController]
[Route("api/exception-rules")]
[Authorize(Roles = "Admin,Dispatcher")]
public class ExceptionRuleController : ApiControllerBase
{
    private readonly IExceptionRuleService _ruleService;

    public ExceptionRuleController(IExceptionRuleService ruleService)
    {
        _ruleService = ruleService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ExceptionRuleDto>>> GetAll()
        => Ok(await _ruleService.GetAllAsync());

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ExceptionRuleDto>> GetById(Guid id)
    {
        var rule = await _ruleService.GetByIdAsync(id);
        if (rule is null)
            return NotFound();
        return Ok(rule);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ExceptionRuleDto>> Create(CreateExceptionRuleRequest request)
    {
        var createdBy = CurrentUserId();
        if (createdBy is null)
            return Unauthorized();

        var result = await _ruleService.CreateAsync(request, createdBy.Value);
        if (result is null)
            return BadRequest(new { message = "Could not save this exception rule." });

        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ExceptionRuleDto>> Update(Guid id, UpdateExceptionRuleRequest request)
    {
        var result = await _ruleService.UpdateAsync(id, request);
        if (result is null)
            return NotFound(new { message = "Rule not found." });

        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _ruleService.DeleteAsync(id);
        if (!deleted)
            return NotFound(new { message = "Rule not found." });
        return NoContent();
    }
}