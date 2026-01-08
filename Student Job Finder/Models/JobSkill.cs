namespace Student_Job_Finder.Models
{
    public class JobSkill
    {
        public int JobSkillId { get; set; }
        public int JobPostId { get; set; }
        public string SkillName { get; set; } = "";
        public decimal SkillScore { get; set; }
    }
}
