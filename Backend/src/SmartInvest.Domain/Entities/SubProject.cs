namespace SmartInvest.Domain.Entities
{
    public class SubProject
    {
        [Key]
        public int SubProjectId { get; set; }

        public int MainProjectId { get; set; }
        public virtual MainProject MainProject { get; set; }
        public string SubProjectName { get; set; } = string.Empty;
        public string ProjectLevel { get; set; } = string.Empty;
        public string ComponentType { get; set; } = string.Empty;
        public string AccountingUnit { get; set; } = string.Empty;
        [NotMapped]
        public decimal TotalCost => BankFunding + SelfFunding; 
        public string ProjectNature { get; set; } = string.Empty;

        // Nullables based on ERD
        public string? GreenInvestmentLink { get; set; } 
        public string? ProjectDescription { get; set; }
        public string? ProjectGoal { get; set; }
        public string? SocialImpact { get; set; }
        public string? EconomicImpact { get; set; }
        public string? EnvironmentalImpact { get; set; }

        [ForeignKey("Markaz")]
        public int MarkazId { get; set; }
        public virtual Markaz Markaz { get; set; }

        [ForeignKey("Priority")]
        public int PriorityId { get; set; }
        public virtual ProjectPriority Priority { get; set; }

        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        [ForeignKey("Status")]
        public int StatusId { get; set; }
        
        [MaxLength(50)]
        public string? SubProjectCode { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal BankFunding { get; set; }  

        [Column(TypeName = "decimal(18,2)")]
        public decimal SelfFunding { get; set; } 
        public virtual ProjectStatus Status { get; set; }

        [ForeignKey("ExecutiveAgency")]
        public int? ExecutiveAgencyId { get; set; }
        public virtual ExecutiveAgency? ExecutiveAgency { get; set; }

        public virtual ICollection<PlanProject> PlanProjects { get; set; }
        public virtual ICollection<ProjectAssignment>? ProjectAssignments { get; set; }
        public virtual ICollection<ProjectSpecification>? ProjectSpecifications { get; set; }
    }
}
