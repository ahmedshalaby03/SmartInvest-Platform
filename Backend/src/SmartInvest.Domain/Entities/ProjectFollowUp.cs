using SmartInvest.Domain.Enums;

namespace SmartInvest.Domain.Entities
{
    public class ProjectFollowUp
    {
        [Key]
        public int FollowUpId { get; set; }

        [ForeignKey("PlanProject")]
        public int PlanProjectId { get; set; }
        public virtual PlanProject PlanProject { get; set; }
        [ForeignKey("Status")]
        public int StatusId { get; set; }
        public virtual ProjectStatus Status { get; set; }
        [ForeignKey("DelayReason")]
        public int? DelayReasonId { get; set; } // Nullable based on ERD
        public virtual DelayReason DelayReason { get; set; }

        public decimal ProgressPercentage { get; set; }
        public DateTime FollowUpDate { get; set; }
        public string? Notes { get; set; }

        public virtual ICollection<ProjectAttachment> Attachments { get; set; }
    }
}
