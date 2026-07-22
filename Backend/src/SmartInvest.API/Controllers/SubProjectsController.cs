using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartInvest.Application.DTOs;
using SmartInvest.Application.DTOs.Common;
using SmartInvest.Application.Interfaces;
using SmartInvest.Domain.Common;

namespace SmartInvest.API.Controllers;

[ApiController]
[Route("api/subprojects")]
[Authorize]
public class SubProjectsController : ControllerBase
{
    private readonly ISubProjectService _subProjectService;

    public SubProjectsController(ISubProjectService subProjectService)
    {
        _subProjectService = subProjectService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResultDto<SubProjectListItemDto>>> Search(
        [FromQuery] int? mainProjectId,
        [FromQuery] int? mainProgramId,
        [FromQuery] int? subProgramId,
        [FromQuery] int? markazId,
        [FromQuery] int? priorityId,
        [FromQuery] int? statusId,
        [FromQuery] string? searchTerm,
        [FromQuery] int page,
        [FromQuery] int pageSize,
        CancellationToken cancellationToken)
    {
        var effectivePage = page <= 0 ? 1 : page;
        var effectivePageSize = pageSize <= 0 ? 20 : pageSize;

        var result = await _subProjectService.SearchAsync(
            mainProjectId, mainProgramId, subProgramId, markazId,
            priorityId, statusId, searchTerm, effectivePage, effectivePageSize, cancellationToken);

        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<SubProjectDetailDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await _subProjectService.GetByIdAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<SubProjectDetailDto>> Create(CreateSubProjectDto dto, CancellationToken cancellationToken)
    {
        var result = await _subProjectService.CreateAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<SubProjectDetailDto>> Update(int id, UpdateSubProjectDto dto, CancellationToken cancellationToken)
    {
        var result = await _subProjectService.UpdateAsync(id, dto, cancellationToken);
        return Ok(result);
    }

    [HttpPut("{id:int}/executive-agency")]
    [Authorize(Roles = Roles.PlanningStaff)]
    public async Task<ActionResult<SubProjectDetailDto>> AssignExecutiveAgency(int id, AssignExecutiveAgencyDto dto, CancellationToken cancellationToken)
    {
        var result = await _subProjectService.AssignExecutiveAgencyAsync(id, dto.ExecutiveAgencyId, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.PlanningManager)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await _subProjectService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}