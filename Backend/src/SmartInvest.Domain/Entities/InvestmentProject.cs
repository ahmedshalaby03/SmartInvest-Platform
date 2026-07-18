using SmartInvest.Domain.Common;
using SmartInvest.Domain.Enums;

namespace SmartInvest.Domain.Entities;

/// <summary>
/// A sample core entity: an investment opportunity offered by the governorate diwan.
/// </summary>
public class InvestmentProject : BaseEntity
{
    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public decimal TargetAmount { get; set; }

    public string Sector { get; set; } = string.Empty;

    public string Location { get; set; } = string.Empty;

    public ProjectStatus Status { get; set; } = ProjectStatus.Draft;
}
