namespace SmartInvest.Domain.Entities
{
    public class ExecutiveAgency
    {
        [Key]
        public int ExecutiveAgencyId { get; set; }
        public string AgencyName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;

        public virtual ICollection<ProjectAssignment> ProjectAssignments { get; set; }
    }
}
