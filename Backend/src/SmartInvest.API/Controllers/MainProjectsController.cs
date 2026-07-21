using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartInvest.Application.DTOs;
using SmartInvest.Application.Interfaces;
using SmartInvest.Domain.Common;

namespace SmartInvest.API.Controllers;

[ApiController]
[Route("api/mainprojects")]
[Authorize]
public class MainProjectsController : ControllerBase
{
    private readonly IMainProjectService _mainProjectService;

    public MainProjectsController(IMainProjectService mainProjectService)
    {
        _mainProjectService = mainProjectService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<MainProjectListItemDto>>> GetAll(CancellationToken cancellationToken)
    {
        var result = await _mainProjectService.GetAllAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<MainProjectDetailDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await _mainProjectService.GetByIdAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<MainProjectDetailDto>> Create(CreateMainProjectDto dto, CancellationToken cancellationToken)
    {
        var result = await _mainProjectService.CreateAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<MainProjectDetailDto>> Update(int id, UpdateMainProjectDto dto, CancellationToken cancellationToken)
    {
        var result = await _mainProjectService.UpdateAsync(id, dto, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.PlanningManager)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await _mainProjectService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}