using SmartInvest.Domain.Common;

namespace SmartInvest.Domain.Entities;

public class InvestmentProject : BaseEntity
{
    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public decimal TargetAmount { get; set; }

    public string Sector { get; set; } = string.Empty;

    public string Location { get; set; } = string.Empty;

    public SmartInvest.Domain.Enums.ProjectStatus Status { get; set; }
        = SmartInvest.Domain.Enums.ProjectStatus.Draft;
}