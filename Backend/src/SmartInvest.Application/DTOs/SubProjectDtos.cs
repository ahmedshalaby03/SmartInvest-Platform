namespace SmartInvest.Application.DTOs;

public class SubProjectListItemDto
{
    public int Id { get; set; }
    public string? Code { get; set; }
    public string Name { get; set; } = string.Empty;
    public int MainProjectId { get; set; }
    public string MainProjectCode { get; set; } = string.Empty;
    public string MainProjectName { get; set; } = string.Empty;
    public string ProjectLevel { get; set; } = string.Empty;
    public string ComponentType { get; set; } = string.Empty;
    public int VillageId { get; set; }
    public string VillageName { get; set; } = string.Empty;
    public int PriorityId { get; set; }
    public string PriorityName { get; set; } = string.Empty;
    public int StatusId { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public decimal BankFunding { get; set; }
    public decimal SelfFunding { get; set; }
    public decimal TotalCost { get; set; }
}

public class SubProjectDetailDto
{
    public int Id { get; set; }
    public string? Code { get; set; }
    public string Name { get; set; } = string.Empty;
    public int MainProjectId { get; set; }
    public string MainProjectName { get; set; } = string.Empty;
    public string ProjectLevel { get; set; } = string.Empty;
    public string ComponentType { get; set; } = string.Empty;
    public string AccountingUnit { get; set; } = string.Empty;
    public string ProjectNature { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Goal { get; set; }
    public string? SocialImpact { get; set; }
    public string? EconomicImpact { get; set; }
    public string? EnvironmentalImpact { get; set; }
    public string? GreenInvestmentLink { get; set; }
    public int VillageId { get; set; }
    public string VillageName { get; set; } = string.Empty;
    public int MarkazId { get; set; }
    public string MarkazName { get; set; } = string.Empty;
    public int GovernorateId { get; set; }
    public string GovernorateName { get; set; } = string.Empty;
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public int PriorityId { get; set; }
    public string PriorityName { get; set; } = string.Empty;
    public int StatusId { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public decimal BankFunding { get; set; }
    public decimal SelfFunding { get; set; }
    public decimal TotalCost { get; set; }
    public IReadOnlyList<ProjectSpecificationDto> Specifications { get; set; } = new List<ProjectSpecificationDto>();
}

public class CreateSubProjectDto
{
    public int MainProjectId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ProjectLevel { get; set; } = string.Empty;
    public string ComponentType { get; set; } = string.Empty;
    public string AccountingUnit { get; set; } = string.Empty;
    public string ProjectNature { get; set; } = string.Empty;
    public int VillageId { get; set; }
    public int PriorityId { get; set; }
    public int StatusId { get; set; }
    public decimal BankFunding { get; set; }
    public decimal SelfFunding { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? Description { get; set; }
    public string? Goal { get; set; }
    public string? SocialImpact { get; set; }
    public string? EconomicImpact { get; set; }
    public string? EnvironmentalImpact { get; set; }
    public string? GreenInvestmentLink { get; set; }
}

public class UpdateSubProjectDto
{
    public string Name { get; set; } = string.Empty;
    public string ProjectLevel { get; set; } = string.Empty;
    public string ComponentType { get; set; } = string.Empty;
    public string AccountingUnit { get; set; } = string.Empty;
    public string ProjectNature { get; set; } = string.Empty;
    public int VillageId { get; set; }
    public int PriorityId { get; set; }
    public int StatusId { get; set; }
    public decimal BankFunding { get; set; }
    public decimal SelfFunding { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? Description { get; set; }
    public string? Goal { get; set; }
    public string? SocialImpact { get; set; }
    public string? EconomicImpact { get; set; }
    public string? EnvironmentalImpact { get; set; }
    public string? GreenInvestmentLink { get; set; }
}