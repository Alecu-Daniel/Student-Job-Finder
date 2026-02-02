namespace Student_Job_Finder.Models
{
    public class JobApplicationViewModel
    {

        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string Email { get; set; } = "";
        public List<StudentSkill> StudentSkills { get; set; } = new();
    }
}
