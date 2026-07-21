namespace SmartInvest.Domain.Entities
{
    public class Village
    {
        [Key]
        public int VillageId { get; set; }
        public string VillageName { get; set; } = string.Empty;
        [ForeignKey("Markaz")]
        public int MarkazId { get; set; }
        public virtual Markaz Markaz { get; set; }
    }
}
