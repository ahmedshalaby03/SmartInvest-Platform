namespace SmartInvest.Domain.Entities
{
    public class ProjectSpecification
    {
        [Key]
        public int Id { get; set; }
        [ForeignKey("SubProject")]
        public int SubProjectId { get; set; }
        public virtual SubProject SubProject { get; set; }

        public string SpecificationName { get; set; } = string.Empty;
        public string SpecificationValue { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
    }
}
