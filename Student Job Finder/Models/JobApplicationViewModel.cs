using Student_Job_Finder.Services;

namespace Student_Job_Finder.Models
{
    public class JobApplicationViewModel
    {

        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string Email { get; set; } = "";
        public List<StudentSkill> StudentSkills { get; set; } = new();
        public List<JobSkill>   RequiredSkills { get; set; } = new();
        public List<QuizSkill> QuizResults { get; set; } = new();
        public decimal MatchScore
        {
            get
            {
                if (!RequiredSkills.Any()) return 0;
                
                var studentVector = new List<decimal>();
                var jobVector = new List<decimal>();

                foreach(var req in RequiredSkills)
                {
                    jobVector.Add(req.SkillScore);

                    var quizMatch = QuizResults.FirstOrDefault(q => q.SkillName == req.SkillName);
                    var transcriptMatch = StudentSkills.FirstOrDefault(s => s.SkillName == req.SkillName);

                    decimal finalScore = quizMatch?.SkillScore ?? transcriptMatch?.SkillScore ?? 0m;
                    studentVector.Add(finalScore);
                }

                return JobMatchingService.ComputeJobMatchScore(studentVector, jobVector) * 100;
            }
        }

    }
}
