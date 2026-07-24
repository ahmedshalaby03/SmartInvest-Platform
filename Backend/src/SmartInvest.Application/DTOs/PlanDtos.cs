namespace SmartInvest.Application.DTOs;

public class PlanDto
{
    public int Id { get; set; }
    public string PlanName { get; set; } = string.Empty;
    public string PlanStatus { get; set; } = string.Empty;
    public DateTime SuggestionDate { get; set; }
    public DateTime? ApprovalDate { get; set; }
    public int FinancialYearId { get; set; }
    public string FinancialYearName { get; set; } = string.Empty;
}

public class PlanSuggestedProjectDto
{
    public int SubProjectId { get; set; }
    public string SubProjectName { get; set; } = string.Empty;
    public string? SubProjectCode { get; set; }
    public string MainProjectName { get; set; } = string.Empty;
    public decimal BankFunding { get; set; }
    public decimal SelfFunding { get; set; }
    public decimal TotalCost { get; set; }
}

public class PlanDetailDto
{
    public int Id { get; set; }
    public string PlanName { get; set; } = string.Empty;
    public string PlanStatus { get; set; } = string.Empty;
    public DateTime SuggestionDate { get; set; }
    public DateTime? ApprovalDate { get; set; }
    public int FinancialYearId { get; set; }
    public string FinancialYearName { get; set; } = string.Empty;
    public IReadOnlyList<PlanSuggestedProjectDto> SuggestedProjects { get; set; } = new List<PlanSuggestedProjectDto>();
}

public class CreatePlanDto
{
    public string PlanName { get; set; } = string.Empty;
    public int FinancialYearId { get; set; }
}

public class UpdatePlanDto
{
    public string PlanName { get; set; } = string.Empty;
}

public class AddSuggestedProjectDto
{
    public int SubProjectId { get; set; }
}

public class ApprovePlanDto
{
    public DateTime ApprovalDate { get; set; }
}
