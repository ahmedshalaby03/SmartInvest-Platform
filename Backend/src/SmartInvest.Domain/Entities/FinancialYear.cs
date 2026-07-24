using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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

        [Column(TypeName = "decimal(18,2)")]
        public decimal? Budget { get; set; }
        public virtual ICollection<Plan> Plans { get; set; }
        public virtual ICollection<SubProjectFinancialYear> SubProjectFinancialYears { get; set; }
    }
}
