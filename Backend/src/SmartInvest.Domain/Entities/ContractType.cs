namespace SmartInvest.Domain.Entities
{
    public class ContractType
    {
        [Key]
        public int ContractTypeId { get; set; }
        public string ContractName { get; set; } = string.Empty;

        public virtual ICollection<ProjectAssignment> ProjectAssignments { get; set; }
    }
}
