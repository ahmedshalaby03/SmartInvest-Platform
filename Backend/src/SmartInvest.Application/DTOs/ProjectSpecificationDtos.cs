namespace SmartInvest.Application.DTOs;

public class ProjectSpecificationDto
{
    public int Id { get; set; }
    public int SubProjectId { get; set; }
    public string SpecificationName { get; set; } = string.Empty;
    public string SpecificationValue { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
}

public class CreateProjectSpecificationDto
{
    public string SpecificationName { get; set; } = string.Empty;
    public string SpecificationValue { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
}

public class UpdateProjectSpecificationDto
{
    public string SpecificationName { get; set; } = string.Empty;
    public string SpecificationValue { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
}