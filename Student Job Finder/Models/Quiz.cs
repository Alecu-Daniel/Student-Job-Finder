namespace Student_Job_Finder.Models
{
    public class Quiz
    {
        public int QuizId { get; set; }
        public int JobApplicationId { get; set; }
        public int StudentId { get; set; }
        public DateTime CompletedAt { get; set; }
    }
}
