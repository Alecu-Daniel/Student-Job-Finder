namespace Student_Job_Finder.Models
{
    public class AcceptedJobViewModel
    {
        public int JobApplicationId { get; set; }
        public int JobPostId { get; set; }
        public int RecruiterId { get; set; }
        public string PostTitle { get; set; } = "";
        public string RecruiterName { get; set; } = "";
        public string Status { get; set; } = "";
    }
}
