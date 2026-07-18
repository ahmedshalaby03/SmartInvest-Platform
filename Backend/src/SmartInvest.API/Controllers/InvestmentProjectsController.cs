using Microsoft.AspNetCore.Mvc;
using SmartInvest.Application.DTOs;
using SmartInvest.Application.Interfaces;

namespace SmartInvest.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InvestmentProjectsController : ControllerBase
{
    private readonly IInvestmentProjectService _service;

    public InvestmentProjectsController(IInvestmentProjectService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<InvestmentProjectDto>>> GetAll(CancellationToken cancellationToken)
        => Ok(await _service.GetAllAsync(cancellationToken));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<InvestmentProjectDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var project = await _service.GetByIdAsync(id, cancellationToken);
        return project is null ? NotFound() : Ok(project);
    }

    [HttpPost]
    public async Task<ActionResult<InvestmentProjectDto>> Create(
        CreateInvestmentProjectDto dto,
        CancellationToken cancellationToken)
    {
        var created = await _service.CreateAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }
}
