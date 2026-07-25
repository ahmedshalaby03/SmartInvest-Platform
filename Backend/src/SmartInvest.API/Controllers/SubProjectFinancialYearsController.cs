using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartInvest.Application.DTOs;
using SmartInvest.Application.Interfaces;
using SmartInvest.Domain.Common;

namespace SmartInvest.API.Controllers;

[ApiController]
[Route("api/subprojects/{subProjectId:int}/financial-years")]
[Authorize]
public class SubProjectFinancialYearsController : ControllerBase
{
    private readonly ISubProjectFinancialYearService _linkService;

    public SubProjectFinancialYearsController(ISubProjectFinancialYearService linkService)
    {
        _linkService = linkService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SubProjectFinancialYearDto>>> GetAll(int subProjectId, CancellationToken cancellationToken)
    {
        var result = await _linkService.GetForSubProjectAsync(subProjectId, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = Roles.PlanningStaff)]
    public async Task<ActionResult<SubProjectFinancialYearDto>> Link(int subProjectId, LinkFinancialYearDto dto, CancellationToken cancellationToken)
    {
        var result = await _linkService.LinkAsync(subProjectId, dto.FinancialYearId, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{financialYearId:int}")]
    [Authorize(Roles = Roles.PlanningStaff)]
    public async Task<IActionResult> Unlink(int subProjectId, int financialYearId, CancellationToken cancellationToken)
    {
        await _linkService.UnlinkAsync(subProjectId, financialYearId, cancellationToken);
        return NoContent();
    }
}
