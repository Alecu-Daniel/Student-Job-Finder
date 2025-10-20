namespace Student_Job_Finder.Models
{
    public class StudentSkill
    {
        public int StudentSkillId { get; set; }
        public int StudentId { get; set; }
        public string SkillName { get; set; } = "";
        public decimal SkillScore { get; set; }
    }
}
