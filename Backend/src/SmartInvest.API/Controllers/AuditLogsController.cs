using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartInvest.Application.DTOs;
using SmartInvest.Application.Interfaces;
using SmartInvest.Domain.Common;
using SmartInvest.Domain.Interfaces;

namespace SmartInvest.API.Controllers;

[ApiController]
[Route("api/audit-logs")]
[Authorize(Roles = Roles.PlanningManager)]
public class AuditLogsController : ControllerBase
{
    private readonly IGenericRepository<SmartInvest.Domain.Entities.AuditLog> _auditLogRepository;

    public AuditLogsController(IGenericRepository<SmartInvest.Domain.Entities.AuditLog> auditLogRepository)
    {
        _auditLogRepository = auditLogRepository;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AuditLogDto>>> GetAll([FromQuery] string entityName, [FromQuery] int entityId, CancellationToken cancellationToken)
    {
        var logs = await _auditLogRepository.FindAsync(x => x.EntityName == entityName && x.EntityId == entityId, cancellationToken);

        var result = logs
            .OrderByDescending(x => x.ChangedAt)
            .Select(x => new AuditLogDto
            {
                Id = x.AuditLogId,
                EntityName = x.EntityName,
                EntityId = x.EntityId,
                FieldName = x.FieldName,
                OldValue = x.OldValue,
                NewValue = x.NewValue,
                ChangedByUserId = x.ChangedByUserId,
                ChangedAt = x.ChangedAt,
            })
            .ToList();

        return Ok(result);
    }
}
