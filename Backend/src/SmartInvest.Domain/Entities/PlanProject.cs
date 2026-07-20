namespace SmartInvest.Domain.Entities
{
    public class PlanProject
    {
        [Key]
        public int PlanProjectId { get; set; }

        [ForeignKey("Plan")]
        public int PlanId { get; set; }
        public virtual Plan Plan { get; set; }

        [ForeignKey("SubProject")]
        public int SubProjectId { get; set; }
        public virtual SubProject SubProject { get; set; }

        public string ApprovalStatus { get; set; } = string.Empty;

        public virtual ICollection<ProjectFollowUp> ProjectFollowUps { get; set; }
    }
}
