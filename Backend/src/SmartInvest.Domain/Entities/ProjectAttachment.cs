namespace SmartInvest.Domain.Entities
{
    public class ProjectAttachment
    {
        [Key]
        public int AttachmentId { get; set; }
        [ForeignKey("ProjectFollowUp")]
        public int FollowUpId { get; set; }
        public virtual ProjectFollowUp ProjectFollowUp { get; set; }

        public string FilePath { get; set; } = string.Empty;
        public string FileType { get; set; } = string.Empty;
        public DateTime UploadDate { get; set; }
        public string FileName { get; set; } = string.Empty;
    }
}
