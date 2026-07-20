namespace SmartInvest.Domain.Entities
{
    public class Plan
    {
        [Key]
        public int PlanId { get; set; }
        public string PlanName { get; set; } = string.Empty;
        public string PlanStatus { get; set; } = string.Empty;

        [ForeignKey("FinancialYear")]
        public int FinancialYearId { get; set; }
        public virtual FinancialYear FinancialYear { get; set; }

        public virtual ICollection<PlanProject> PlanProjects { get; set; }
    }
}
