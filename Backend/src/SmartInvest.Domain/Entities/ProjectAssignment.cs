namespace SmartInvest.Domain.Entities
{
    public class ProjectAssignment
    {
        [Key]
        public int AssignmentId { get; set; }
        [ForeignKey("SubProject")]
        public int SubProjectId { get; set; }
        public virtual SubProject SubProject { get; set; }
        [ForeignKey("Contractor")]
        public int? ContractorId { get; set; } // Nullable based on ERD because it may not be assigned for now to contractor
        public virtual Contractor Contractor { get; set; }
        [ForeignKey("ContractType")]
        public int ContractTypeId { get; set; }
        public virtual ContractType ContractType { get; set; }

        public DateTime AssignmentDate { get; set; }
        public string? ContractNumber { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal? ContractValue { get; set; }
        public DateTime ExpectedStartDate { get; set; }
        public DateTime ExpectedEndDate { get; set; }
        public string? Notes { get; set; }

        /// <summary>
        /// يُضبط تلقائيًا لـ true لكل التعيينات القائمة عند تغيير الجهة التنفيذية للمشروع الفرعي.
        /// التعيين المقفول للقراءة فقط لأي حد ما عدا مدير التخطيط.
        /// </summary>
        public bool IsLocked { get; set; }
    }
}
