using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartInvest.Application.DTOs;
using SmartInvest.Application.Interfaces;
using SmartInvest.Domain.Common;

namespace SmartInvest.API.Controllers;

[ApiController]
[Route("api/contractors")]
[Authorize(Roles = Roles.StaffAndAgency)]
public class ContractorsController : ControllerBase
{
    private readonly IContractorService _contractorService;

    public ContractorsController(IContractorService contractorService)
    {
        _contractorService = contractorService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ContractorDto>>> GetAll(CancellationToken cancellationToken)
    {
        var result = await _contractorService.GetAllAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ContractorDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await _contractorService.GetByIdAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = Roles.PlanningManager)]
    public async Task<ActionResult<ContractorDto>> Create(CreateContractorDto dto, CancellationToken cancellationToken)
    {
        var result = await _contractorService.CreateAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = Roles.PlanningManager)]
    public async Task<ActionResult<ContractorDto>> Update(int id, UpdateContractorDto dto, CancellationToken cancellationToken)
    {
        var result = await _contractorService.UpdateAsync(id, dto, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.PlanningManager)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await _contractorService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPut("{id:int}/reset-password")]
    [Authorize(Roles = Roles.PlanningManager)]
    public async Task<IActionResult> ResetPassword(int id, ResetPasswordDto dto, CancellationToken cancellationToken)
    {
        await _contractorService.ResetPasswordAsync(id, dto.NewPassword, cancellationToken);
        return NoContent();
    }
}
