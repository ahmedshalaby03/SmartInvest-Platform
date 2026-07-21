using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartInvest.Application.DTOs;
using SmartInvest.Application.Interfaces;

namespace SmartInvest.API.Controllers;

[ApiController]
[Route("api/lookups")]
[Authorize]
public class LookupsController : ControllerBase
{
    private readonly ILookupService _lookupService;

    public LookupsController(ILookupService lookupService)
    {
        _lookupService = lookupService;
    }

    [HttpGet("priorities")]
    public async Task<ActionResult<IReadOnlyList<LookupDto>>> GetPriorities(CancellationToken cancellationToken)
    {
        var result = await _lookupService.GetPrioritiesAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("statuses")]
    public async Task<ActionResult<IReadOnlyList<LookupDto>>> GetStatuses(CancellationToken cancellationToken)
    {
        var result = await _lookupService.GetStatusesAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("main-programs")]
    public async Task<ActionResult<IReadOnlyList<LookupDto>>> GetMainPrograms(CancellationToken cancellationToken)
    {
        var result = await _lookupService.GetMainProgramsAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("sub-programs")]
    public async Task<ActionResult<IReadOnlyList<SubProgramLookupDto>>> GetSubPrograms([FromQuery] int? mainProgramId, CancellationToken cancellationToken)
    {
        var result = await _lookupService.GetSubProgramsAsync(mainProgramId, cancellationToken);
        return Ok(result);
    }

    [HttpGet("governorates")]
    public async Task<ActionResult<IReadOnlyList<LookupDto>>> GetGovernorates(CancellationToken cancellationToken)
    {
        var result = await _lookupService.GetGovernoratesAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("markaz")]
    public async Task<ActionResult<IReadOnlyList<MarkazLookupDto>>> GetMarkaz([FromQuery] int? governorateId, CancellationToken cancellationToken)
    {
        var result = await _lookupService.GetMarkazAsync(governorateId, cancellationToken);
        return Ok(result);
    }

    [HttpGet("villages")]
    public async Task<ActionResult<IReadOnlyList<VillageLookupDto>>> GetVillages([FromQuery] int? markazId, CancellationToken cancellationToken)
    {
        var result = await _lookupService.GetVillagesAsync(markazId, cancellationToken);
        return Ok(result);
    }
}