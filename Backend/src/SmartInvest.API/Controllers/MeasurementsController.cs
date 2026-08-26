using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartInvest.Application.DTOs;
using SmartInvest.Application.Interfaces;
using SmartInvest.Domain.Common;

namespace SmartInvest.API.Controllers;

[ApiController]
[Route("api/measurements")]
[Authorize]
public class MeasurementsController : ControllerBase
{
    private readonly IMeasurementService _measurementService;

    public MeasurementsController(IMeasurementService measurementService)
    {
        _measurementService = measurementService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<MeasurementDto>>> GetAll(CancellationToken cancellationToken)
    {
        var result = await _measurementService.GetAllAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("applicable")]
    public async Task<ActionResult<IReadOnlyList<MeasurementDto>>> GetApplicable([FromQuery] int subProgramId, CancellationToken cancellationToken)
    {
        var result = await _measurementService.GetApplicableForSubProgramAsync(subProgramId, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = Roles.PlanningStaff)]
    public async Task<ActionResult<MeasurementDto>> Create(CreateMeasurementDto dto, CancellationToken cancellationToken)
    {
        var result = await _measurementService.CreateAsync(dto, cancellationToken);
        return Ok(result);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = Roles.PlanningStaff)]
    public async Task<ActionResult<MeasurementDto>> Update(int id, UpdateMeasurementDto dto, CancellationToken cancellationToken)
    {
        var result = await _measurementService.UpdateAsync(id, dto, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.ManagementStaff)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await _measurementService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
