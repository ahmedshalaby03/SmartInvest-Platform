namespace SmartInvest.Domain.Entities
{
    public class AuditLog
    {
        [Key]
        public int AuditLogId { get; set; }

        [Required, MaxLength(100)]
        public string EntityName { get; set; } = string.Empty;

        public int EntityId { get; set; }

        [Required, MaxLength(100)]
        public string FieldName { get; set; } = string.Empty;

        public string? OldValue { get; set; }
        public string? NewValue { get; set; }

        [Required]
        public string ChangedByUserId { get; set; } = string.Empty;

        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
    }
}
