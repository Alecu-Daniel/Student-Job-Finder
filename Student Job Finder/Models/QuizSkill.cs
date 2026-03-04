namespace Student_Job_Finder.Models
{
    public class QuizSkill
    {
        public int QuizSkillId { get; set; }
        public int QuizId { get; set; }
        public string SkillName { get; set; } = "";
        public decimal SkillScore { get; set; }
    }
}
