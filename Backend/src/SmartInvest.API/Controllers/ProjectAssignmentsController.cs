using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartInvest.Application.DTOs;
using SmartInvest.Application.Interfaces;
using SmartInvest.Domain.Common;

namespace SmartInvest.API.Controllers;

[ApiController]
[Route("api/subprojects/{subProjectId:int}/assignments")]
[Authorize]
public class ProjectAssignmentsController : ControllerBase
{
    private readonly IProjectAssignmentService _assignmentService;
    private readonly IChangeRequestService _changeRequestService;

    public ProjectAssignmentsController(IProjectAssignmentService assignmentService, IChangeRequestService changeRequestService)
    {
        _assignmentService = assignmentService;
        _changeRequestService = changeRequestService;
    }

    [HttpGet]
    [Authorize(Roles = Roles.StaffAndAgency)]
    public async Task<ActionResult<IReadOnlyList<ProjectAssignmentDto>>> GetAll(int subProjectId, CancellationToken cancellationToken)
    {
        var result = await _assignmentService.GetBySubProjectAsync(subProjectId, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = Roles.StaffAndAgency)]
    public async Task<ActionResult<ProjectAssignmentDto>> Create(int subProjectId, CreateProjectAssignmentDto dto, CancellationToken cancellationToken)
    {
        var result = await _assignmentService.CreateAsync(subProjectId, dto, cancellationToken);
        return Ok(result);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = Roles.StaffAndAgency)]
    public async Task<ActionResult<ProjectAssignmentDto>> Update(int subProjectId, int id, UpdateProjectAssignmentDto dto, CancellationToken cancellationToken)
    {
        var result = await _assignmentService.UpdateGeneralAsync(subProjectId, id, dto, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.PlanningManager)]
    public async Task<IActionResult> Delete(int subProjectId, int id, CancellationToken cancellationToken)
    {
        await _assignmentService.DeleteAsync(subProjectId, id, cancellationToken);
        return NoContent();
    }

    [HttpGet("{id:int}/change-requests")]
    [Authorize(Roles = Roles.StaffAndAgency)]
    public async Task<ActionResult<IReadOnlyList<ChangeRequestDto>>> GetChangeRequests(int subProjectId, int id, CancellationToken cancellationToken)
    {
        var result = await _changeRequestService.GetHistoryAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:int}/change-requests")]
    [Authorize(Roles = Roles.AssignmentParties)]
    public async Task<ActionResult<ChangeRequestDto>> SubmitChangeRequest(int subProjectId, int id, CreateChangeRequestDto dto, CancellationToken cancellationToken)
    {
        var result = await _changeRequestService.SubmitAsync(id, dto, cancellationToken);
        return Ok(result);
    }

    [HttpPut("{id:int}/change-requests/{changeRequestId:int}/approve")]
    [Authorize(Roles = Roles.StaffAndAgency)]
    public async Task<ActionResult<ChangeRequestDto>> ApproveChangeRequest(int subProjectId, int id, int changeRequestId, ReviewChangeRequestDto dto, CancellationToken cancellationToken)
    {
        var result = await _changeRequestService.ApproveAsync(id, changeRequestId, dto, cancellationToken);
        return Ok(result);
    }

    [HttpPut("{id:int}/change-requests/{changeRequestId:int}/reject")]
    [Authorize(Roles = Roles.StaffAndAgency)]
    public async Task<ActionResult<ChangeRequestDto>> RejectChangeRequest(int subProjectId, int id, int changeRequestId, ReviewChangeRequestDto dto, CancellationToken cancellationToken)
    {
        var result = await _changeRequestService.RejectAsync(id, changeRequestId, dto, cancellationToken);
        return Ok(result);
    }
}
