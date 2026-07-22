using Microsoft.AspNetCore.Identity;
using SmartInvest.Domain.Entities;

namespace SmartInvest.Infrastructure.Identity;

/// <summary>
/// Application user extending ASP.NET Core Identity.
/// </summary>
public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>مضبوطة فقط لمستخدمي دور ExecutiveAgency — حساب واحد لكل جهة.</summary>
    public int? ExecutiveAgencyId { get; set; }
    public virtual ExecutiveAgency? ExecutiveAgency { get; set; }

    /// <summary>مضبوطة فقط لمستخدمي دور Contractor — حساب واحد لكل مقاول.</summary>
    public int? ContractorId { get; set; }
    public virtual Contractor? Contractor { get; set; }
}
