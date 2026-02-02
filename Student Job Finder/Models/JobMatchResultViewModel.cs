namespace Student_Job_Finder.Models
{
    public class JobMatchResultViewModel
    {
        public JobPost Job { get; set; } = null!;
        public decimal Similarity { get; set; }
        public List<JobSkill> JobSkills { get; set; } = new();
        public List<UnderqualifiedSkillViewModel> UnderqualifiedSkills { get; set; } = new();
    }
}
