namespace SmartInvest.Domain.Entities
{
    public class ProjectPriority
    {
        [Key]
        public int Id { get; set; }
        public string Priority { get; set; } = string.Empty;
        public virtual ICollection<SubProject> SubProjects { get; set; }
    }
}
