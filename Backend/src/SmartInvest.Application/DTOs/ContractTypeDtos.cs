namespace SmartInvest.Application.DTOs;

public class ContractTypeDto
{
    public int Id { get; set; }
    public string ContractName { get; set; } = string.Empty;
}

public class CreateContractTypeDto
{
    public string ContractName { get; set; } = string.Empty;
}

public class UpdateContractTypeDto
{
    public string ContractName { get; set; } = string.Empty;
}
