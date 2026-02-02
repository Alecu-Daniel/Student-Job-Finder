namespace Student_Job_Finder.Models
{
    public class JobApplication
    {
        public int JobApplicationId { get; set; }
        public int JobPostId { get; set; }
        public int StudentId { get; set; }
        public string? Message { get; set; }
        public DateTime AppliedAt { get; set; }
        public string Status { get; set; } = "Pending";
    }
}
