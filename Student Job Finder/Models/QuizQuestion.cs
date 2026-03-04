namespace Student_Job_Finder.Models
{
    public class QuizQuestion
    {
        public int QuestionId { get; set; }
        public int JobPostId { get; set; }
        public string SkillName { get; set; } = "";
        public string QuestionText { get; set; } = "";
        public string OptionA { get; set; } = "";
        public string OptionB { get; set; } = "";
        public string OptionC { get; set; } = "";
        public string OptionD { get; set; } = "";
        public string CorrectOption { get; set; } = "";
        public string? StudentAnswer { get; set; }
    }
}
