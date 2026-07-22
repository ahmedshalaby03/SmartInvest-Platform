namespace SmartInvest.Application.Interfaces;

/// <summary>
/// آلية تسجيل تعديلات عامة، قابلة لإعادة الاستخدام عبر أي كيان (تُستخدم هنا لـ ProjectAssignment،
/// ومخطط استخدامها في مسار متابعة المشروع لاحقًا).
/// </summary>
public interface IAuditLogService
{
    Task LogAsync(string entityName, int entityId, string fieldName, string? oldValue, string? newValue, string changedByUserId, CancellationToken cancellationToken = default);
}
