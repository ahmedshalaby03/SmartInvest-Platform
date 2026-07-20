namespace SmartInvest.Domain.Entities
{
    public class ProjectAssignment
    {
        [Key]
        public int AssignmentId { get; set; }
        [ForeignKey("SubProject")]
        public int SubProjectId { get; set; }
        public virtual SubProject SubProject { get; set; }
        [ForeignKey("Contractor")]
        public int? ContractorId { get; set; } // Nullable based on ERD because it may not be assigned for now to contractor 
        public virtual Contractor Contractor { get; set; }
        [ForeignKey("ContractType")]
        public int ContractTypeId { get; set; }
        public virtual ContractType ContractType { get; set; }

        [ForeignKey("ExecutiveAgency")]
        public int ExecutiveAgencyId { get; set; }
        public virtual ExecutiveAgency ExecutiveAgency { get; set; }

        public DateTime AssignmentDate { get; set; }
        public string? ContractNumber { get; set; } 
        public decimal? ContractValue { get; set; }
        public DateTime ExpectedStartDate { get; set; }
        public DateTime ExpectedEndDate { get; set; }
        public string? Notes { get; set; } 
    }
}
