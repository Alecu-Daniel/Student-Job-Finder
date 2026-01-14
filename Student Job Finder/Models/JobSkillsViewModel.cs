namespace Student_Job_Finder.Models
{
    public class JobSkillsViewModel
    {
        public int PostId { get; set; }
        public string PostTitle { get; set; } = "";
        public string PostContent { get; set; } = "";
        public decimal Price { get; set; }
        public string PricePeriod { get; set; } = "";

        public List<JobSkill> Skills { get; set; } = new();
    }
}
