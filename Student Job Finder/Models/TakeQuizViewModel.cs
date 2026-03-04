namespace Student_Job_Finder.Models
{
    public class TakeQuizViewModel
    {
        public int QuizId { get; set; }
        public int JobPostId { get; set; }
        public List<QuizQuestion> Questions { get; set; } = new();
    }
}
