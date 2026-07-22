namespace SmartInvest.Application.DTOs;

public class ProjectAssignmentDto
{
    public int Id { get; set; }
    public int SubProjectId { get; set; }
    public int ExecutiveAgencyId { get; set; }
    public string ExecutiveAgencyName { get; set; } = string.Empty;
    public int? ContractorId { get; set; }
    public string? ContractorName { get; set; }
    public int ContractTypeId { get; set; }
    public string ContractTypeName { get; set; } = string.Empty;
    public DateTime AssignmentDate { get; set; }
    public string? ContractNumber { get; set; }
    public decimal? ContractValue { get; set; }
    public DateTime ExpectedStartDate { get; set; }
    public DateTime ExpectedEndDate { get; set; }
    public string? Notes { get; set; }
    public bool IsLocked { get; set; }
}

public class CreateProjectAssignmentDto
{
    public int? ContractorId { get; set; }
    public int ContractTypeId { get; set; }
    public string? ContractNumber { get; set; }
    public decimal? ContractValue { get; set; }
    public DateTime ExpectedStartDate { get; set; }
    public DateTime ExpectedEndDate { get; set; }
    public string? Notes { get; set; }
}

public class UpdateProjectAssignmentDto
{
    public int? ContractorId { get; set; }
    public int ContractTypeId { get; set; }
    public string? ContractNumber { get; set; }
    public string? Notes { get; set; }
}
