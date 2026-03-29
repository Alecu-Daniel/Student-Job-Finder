namespace Student_Job_Finder.Models
{
    public class Feedback
    {
        public int FeedbackId { get; set; }
        public int JobApplicationId { get; set; }
        public int JobPostId { get; set; }
        public string PostTitle { get; set; } = "";
        public int StudentId { get; set; }
        public int RecruiterId { get; set; }
        public int Rating { get; set; }
        public string Comment { get; set; } = "";
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
