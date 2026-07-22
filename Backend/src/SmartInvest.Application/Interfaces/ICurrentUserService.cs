namespace SmartInvest.Application.Interfaces;

/// <summary>
/// يكشف هوية المستخدم الحالي (من الـ JWT) للطبقات الأعلى بدون أي اعتماد مباشر على ASP.NET Core.
/// </summary>
public interface ICurrentUserService
{
    string? UserId { get; }

    string? Role { get; }

    int? ExecutiveAgencyId { get; }

    int? ContractorId { get; }
}
