using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartInvest.Application.DTOs;
using SmartInvest.Application.Interfaces;
using SmartInvest.Domain.Common;

namespace SmartInvest.API.Controllers;

[ApiController]
[Route("api/agencies")]
[Authorize(Roles = Roles.PlanningStaff)]
public class ExecutiveAgenciesController : ControllerBase
{
    private readonly IExecutiveAgencyService _agencyService;

    public ExecutiveAgenciesController(IExecutiveAgencyService agencyService)
    {
        _agencyService = agencyService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ExecutiveAgencyDto>>> GetAll(CancellationToken cancellationToken)
    {
        var result = await _agencyService.GetAllAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ExecutiveAgencyDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await _agencyService.GetByIdAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = Roles.PlanningManager)]
    public async Task<ActionResult<ExecutiveAgencyDto>> Create(CreateExecutiveAgencyDto dto, CancellationToken cancellationToken)
    {
        var result = await _agencyService.CreateAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = Roles.PlanningManager)]
    public async Task<ActionResult<ExecutiveAgencyDto>> Update(int id, UpdateExecutiveAgencyDto dto, CancellationToken cancellationToken)
    {
        var result = await _agencyService.UpdateAsync(id, dto, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.PlanningManager)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await _agencyService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPut("{id:int}/reset-password")]
    [Authorize(Roles = Roles.PlanningManager)]
    public async Task<IActionResult> ResetPassword(int id, ResetPasswordDto dto, CancellationToken cancellationToken)
    {
        await _agencyService.ResetPasswordAsync(id, dto.NewPassword, cancellationToken);
        return NoContent();
    }
}
