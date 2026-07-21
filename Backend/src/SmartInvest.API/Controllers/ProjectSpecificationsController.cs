using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartInvest.Application.DTOs;
using SmartInvest.Application.Interfaces;

namespace SmartInvest.API.Controllers;

[ApiController]
[Route("api/subprojects/{subProjectId:int}/specifications")]
[Authorize]
public class ProjectSpecificationsController : ControllerBase
{
    private readonly IProjectSpecificationService _specificationService;

    public ProjectSpecificationsController(IProjectSpecificationService specificationService)
    {
        _specificationService = specificationService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ProjectSpecificationDto>>> GetAll(int subProjectId, CancellationToken cancellationToken)
    {
        var result = await _specificationService.GetBySubProjectAsync(subProjectId, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<ProjectSpecificationDto>> Create(int subProjectId, CreateProjectSpecificationDto dto, CancellationToken cancellationToken)
    {
        var result = await _specificationService.CreateAsync(subProjectId, dto, cancellationToken);
        return Ok(result);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ProjectSpecificationDto>> Update(int subProjectId, int id, UpdateProjectSpecificationDto dto, CancellationToken cancellationToken)
    {
        var result = await _specificationService.UpdateAsync(id, dto, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int subProjectId, int id, CancellationToken cancellationToken)
    {
        await _specificationService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}