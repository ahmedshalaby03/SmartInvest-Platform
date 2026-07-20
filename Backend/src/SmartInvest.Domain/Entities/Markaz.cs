namespace SmartInvest.Domain.Entities
{
    public class Markaz
    {
        [Key]
        public int MarkazId { get; set; }
        public string MarkazName { get; set; } = string.Empty;

        [ForeignKey("Governorate")]
        public int GovernorateId { get; set; }
        public virtual Governorate Governorate { get; set; } 
        public virtual ICollection<Village> Villages { get; set; }
    }
}
