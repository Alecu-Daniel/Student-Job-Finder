namespace Student_Job_Finder.Dtos
{
    public class QuizSubmissionDto
    {
        public int QuizId { get; set; }
        public List<QuestionAnswerDto> Answers { get; set; } = new();
    }
}
