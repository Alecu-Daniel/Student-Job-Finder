namespace Student_Job_Finder.Dtos
{
    public class JobPostToAddDto
    {
        public string PostTitle { get; set; } = "";
        public string PostContent { get; set; } = "";

        public decimal Price { get; set; }
        public string PricePeriod { get; set; } = "";
        
    }
}
