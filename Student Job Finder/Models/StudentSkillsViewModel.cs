namespace Student_Job_Finder.Models
{
    public class StudentSkillsViewModel
    {
        public List<StudentSkill> StudentSkills { get; set; } = new();
        public Dictionary<string,int> PotentialJobs { get; set; } = new();
    }
}
