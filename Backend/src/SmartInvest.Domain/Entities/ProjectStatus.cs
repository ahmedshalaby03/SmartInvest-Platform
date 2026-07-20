namespace SmartInvest.Domain.Entities
{
    public class ProjectStatus
    {
        [Key]
        public int StatusId { get; set; }
        public string StatusName { get; set; } = string.Empty;

        public virtual ICollection<SubProject> SubProjects { get; set; }
        public virtual ICollection<ProjectFollowUp> ProjectFollowUps { get; set; }
    }
}
