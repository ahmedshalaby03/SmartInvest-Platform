namespace SmartInvest.Domain.Entities
{
    public class DelayReason
    {
        [Key]
        public int Id { get; set; }
        public string Reason { get; set; } = string.Empty;
        public virtual ICollection<ProjectFollowUp> ProjectFollowUps { get; set; }
    }
}
