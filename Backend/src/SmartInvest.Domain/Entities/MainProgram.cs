namespace SmartInvest.Domain.Entities
{
    public class MainProgram
    {
        [Key]
        public int ProgramId { get; set; }
        public string ProgramName { get; set; } = string.Empty;
        public virtual ICollection<SubProgram> SubPrograms { get; set; }
    }
}
