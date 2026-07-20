namespace SmartInvest.Domain.Entities
{
    public class Notification
    {
        [Key]
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string NotificationType { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string ReceiverRole { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsRead { get; set; }
        public int RelatedEntityId { get; set; }
    }
}
