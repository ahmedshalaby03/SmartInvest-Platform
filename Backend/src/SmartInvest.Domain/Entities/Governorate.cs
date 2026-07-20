namespace SmartInvest.Domain.Entities
{
    public class Governorate
    {
        [Key]
        public int GovernorateId { get; set; }
        public string GovernorateName { get; set; } =string.Empty;
        public virtual ICollection<Markaz> Marakez { get; set; }
    }
}
