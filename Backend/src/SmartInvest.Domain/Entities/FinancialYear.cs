namespace SmartInvest.Domain.Entities
{
    public class FinancialYear
    {
        [Key]
        public int FinancialYearId { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsClosed { get; set; }
        public virtual ICollection<Plan> Plans { get; set; }
    }
}
