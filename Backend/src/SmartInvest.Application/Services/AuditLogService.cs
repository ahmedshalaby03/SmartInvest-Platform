using SmartInvest.Application.Interfaces;
using SmartInvest.Domain.Entities;
using SmartInvest.Domain.Interfaces;

namespace SmartInvest.Application.Services;

public class AuditLogService : IAuditLogService
{
    private readonly IGenericRepository<AuditLog> _auditLogRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AuditLogService(IGenericRepository<AuditLog> auditLogRepository, IUnitOfWork unitOfWork)
    {
        _auditLogRepository = auditLogRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task LogAsync(string entityName, int entityId, string fieldName, string? oldValue, string? newValue, string changedByUserId, CancellationToken cancellationToken = default)
    {
        var entry = new AuditLog
        {
            EntityName = entityName,
            EntityId = entityId,
            FieldName = fieldName,
            OldValue = oldValue,
            NewValue = newValue,
            ChangedByUserId = changedByUserId,
            ChangedAt = DateTime.UtcNow,
        };

        await _auditLogRepository.AddAsync(entry, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
