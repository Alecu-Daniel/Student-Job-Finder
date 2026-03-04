namespace Student_Job_Finder.Dtos
{
    public class QuizQuestionToAddDto
    {
        public int JobPostId { get; set; }
        public string SkillName { get; set; } = "";
        public string QuestionText { get; set; } = "";
        public string OptionA { get; set; } = "";
        public string OptionB { get; set; } = "";
        public string OptionC { get; set; } = "";
        public string OptionD { get; set; } = "";
        public string CorrectOption { get; set; } = "";
    }
}
