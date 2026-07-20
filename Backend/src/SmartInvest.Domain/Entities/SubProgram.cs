namespace SmartInvest.Domain.Entities
{
    public class SubProgram
    {
        [Key]
        public int SubProgramId { get; set; }
        public string SubProgramName { get; set; } = string.Empty;

        [ForeignKey("MainProgram")]
        public int ProgramId { get; set; }
        public virtual MainProgram MainProgram { get; set; }
        public virtual ICollection<MainProject> MainProjects { get; set; }
    }
}
