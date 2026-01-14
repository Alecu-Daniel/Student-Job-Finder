using Microsoft.AspNetCore.Mvc;

namespace Student_Job_Finder.Models
{
    public class JobPost
    {
        public int PostId { get; set; }
        public int UserId { get; set; }
        public string PostTitle { get; set; } = "";
        public string PostContent { get; set; } = "";
        public decimal Price { get; set; }
        public string PricePeriod { get; set; } = "";
        public DateTime PostCreated { get; set; }
        public DateTime PostUpdated { get; set; }
    }
}
