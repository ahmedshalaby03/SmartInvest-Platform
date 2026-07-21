namespace SmartInvest.Application.DTOs;

public class MainProjectListItemDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ExecutingAgency { get; set; } = string.Empty;
    public int SubProgramId { get; set; }
    public string SubProgramName { get; set; } = string.Empty;
    public string MainProgramName { get; set; } = string.Empty;
    public int SubProjectsCount { get; set; }
    public decimal TotalBankFunding { get; set; }
    public decimal TotalSelfFunding { get; set; }
}

public class MainProjectDetailDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ExecutingAgency { get; set; } = string.Empty;
    public int SubProgramId { get; set; }
    public string SubProgramName { get; set; } = string.Empty;
    public string MainProgramName { get; set; } = string.Empty;
    public IReadOnlyList<SubProjectListItemDto> SubProjects { get; set; } = new List<SubProjectListItemDto>();
}

public class CreateMainProjectDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ExecutingAgency { get; set; } = string.Empty;
    public int SubProgramId { get; set; }
}

public class UpdateMainProjectDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ExecutingAgency { get; set; } = string.Empty;
    public int SubProgramId { get; set; }
}
