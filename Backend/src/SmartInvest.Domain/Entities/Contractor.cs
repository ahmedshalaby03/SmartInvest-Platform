namespace SmartInvest.Domain.Entities
{
    public class Contractor
    {
        [Key]
        public int ContractorId { get; set; }
        public string ContractorName { get; set; } = string.Empty;
        public string CompanyType { get; set; } = string.Empty;
        public string NationalIdOrCommercialRegister { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public bool IsActive { get; set; }

        public virtual ICollection<ProjectAssignment> ProjectAssignments { get; set; }
    }
}
