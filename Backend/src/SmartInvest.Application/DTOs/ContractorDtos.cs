namespace SmartInvest.Application.DTOs;

public class ContractorDto
{
    public int Id { get; set; }
    public string ContractorName { get; set; } = string.Empty;
    public string CompanyType { get; set; } = string.Empty;
    public string NationalIdOrCommercialRegister { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string UserName { get; set; } = string.Empty;
}

public class CreateContractorDto
{
    public string ContractorName { get; set; } = string.Empty;
    public string CompanyType { get; set; } = string.Empty;
    public string NationalIdOrCommercialRegister { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class UpdateContractorDto
{
    public string ContractorName { get; set; } = string.Empty;
    public string CompanyType { get; set; } = string.Empty;
    public string NationalIdOrCommercialRegister { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
