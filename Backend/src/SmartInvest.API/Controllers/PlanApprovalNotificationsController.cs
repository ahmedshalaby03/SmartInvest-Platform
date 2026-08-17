using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartInvest.Application.Interfaces;
using SmartInvest.Domain.Common;
using SmartInvest.Domain.Enums;

namespace SmartInvest.API.Controllers;

[ApiController]
[Route("api/plan-approval-notifications")]
[Authorize(Roles = Roles.SuperAdmin)]
public class PlanApprovalNotificationsController : ControllerBase
{
    // Limit the file size to 10 MB
    private readonly IPlanApprovalNotificationService _service;

    public PlanApprovalNotificationsController(IPlanApprovalNotificationService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> Get(
        int page = 1,
        int pageSize = 20,
        PlanApprovalNotificationStatus? status = null,
        int? financialYearId = null,
        string? planName = null,
        DateTime? fromUtc = null,
        DateTime? toUtc = null,
        CancellationToken cancellationToken = default)
    {
        return Ok(await _service.GetAsync(page, pageSize, status, financialYearId, planName, fromUtc, toUtc, cancellationToken));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        return Ok(await _service.GetByIdAsync(id, cancellationToken));
    }

    [HttpPost("{id:int}/retry")]
    public async Task<IActionResult> Retry(int id, CancellationToken cancellationToken)
    {
        await _service.RetryAsync(id, cancellationToken);
        return NoContent();
    }
}
