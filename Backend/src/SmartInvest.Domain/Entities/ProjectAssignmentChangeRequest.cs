using SmartInvest.Domain.Enums;

namespace SmartInvest.Domain.Entities
{
    public class ProjectAssignmentChangeRequest
    {
        [Key]
        public int ChangeRequestId { get; set; }

        [ForeignKey("Assignment")]
        public int AssignmentId { get; set; }
        public virtual ProjectAssignment Assignment { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? RequestedContractValue { get; set; }
        public DateTime? RequestedExpectedEndDate { get; set; }

        public ChangeRequestStatus Status { get; set; } = ChangeRequestStatus.Pending;

        [Required]
        public string RequestedByUserId { get; set; } = string.Empty;
        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

        public string? ReviewedByUserId { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public string? ReviewNote { get; set; }
    }
}
