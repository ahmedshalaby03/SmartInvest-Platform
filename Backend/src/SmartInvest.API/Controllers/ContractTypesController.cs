using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartInvest.Application.DTOs;
using SmartInvest.Application.Interfaces;
using SmartInvest.Domain.Common;

namespace SmartInvest.API.Controllers;

[ApiController]
[Route("api/contract-types")]
[Authorize(Roles = Roles.PlanningStaff)]
public class ContractTypesController : ControllerBase
{
    private readonly IContractTypeService _contractTypeService;

    public ContractTypesController(IContractTypeService contractTypeService)
    {
        _contractTypeService = contractTypeService;
    }

    [HttpGet]
    [Authorize]
    public async Task<ActionResult<IReadOnlyList<ContractTypeDto>>> GetAll(CancellationToken cancellationToken)
    {
        var result = await _contractTypeService.GetAllAsync(cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<ContractTypeDto>> Create(CreateContractTypeDto dto, CancellationToken cancellationToken)
    {
        var result = await _contractTypeService.CreateAsync(dto, cancellationToken);
        return Ok(result);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ContractTypeDto>> Update(int id, UpdateContractTypeDto dto, CancellationToken cancellationToken)
    {
        var result = await _contractTypeService.UpdateAsync(id, dto, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.PlanningManager)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await _contractTypeService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
