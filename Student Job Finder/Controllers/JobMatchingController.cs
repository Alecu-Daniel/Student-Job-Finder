using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Student_Job_Finder.Data;
using Student_Job_Finder.Models;
using Student_Job_Finder.Services;

namespace Student_Job_Finder.Controllers
{


    [Authorize]
    [Route("[controller]")]
    public class JobMatchingController : Controller
    {

        private readonly DataContextDapper _dapper;
        public JobMatchingController(IConfiguration config,JobMatchingService matchingService)
        {
            _dapper = new DataContextDapper(config);
        }

        private readonly List<string> allSkills = new List<string>
        {
            "Software Development",
            "Algorithms and Data Structures",
            "Computer Architecture",
            "Artificial Intelligence",
            "Operating Systems",
            "Database Management",
            "Web Development",
            "Networking",
            "Distributed Systems"
        };



        [HttpGet("MatchJobs")]
        public IActionResult MatchJobsForStudent()
        {
            if (User.FindFirst("userRole")?.Value != "Student")
                return Unauthorized("Only students can use job matching.");

            int userId = int.Parse(User.FindFirst("userId")?.Value ?? "0");

            string studentSql = "SELECT * FROM JobFinderSchema.StudentSkills WHERE StudentId = '" + userId + "'";
            var studentSkills = _dapper.LoadData<StudentSkill>(studentSql);

            string jobsSql = "SELECT * FROM JobFinderSchema.Posts";
            var jobs = _dapper.LoadData<JobPost>(jobsSql).ToList();


            string jobSkillsSql = "SELECT * FROM JobFinderSchema.JobSkills";
            var jobSkills = _dapper.LoadData<JobSkill>(jobSkillsSql);

            //rows = jobs columns = skills
            List<List<decimal>> matrix = new List<List<decimal>>();

            foreach (var job in jobs)
            {
                List<decimal> jobRow = new List<decimal>();

                foreach (var skillName in allSkills)
                {
                    JobSkill? foundSkill = null;

                    // Find the skill for this job
                    foreach (var js in jobSkills)
                    {
                        if (js.JobPostId == job.PostId && js.SkillName == skillName)
                        {
                            foundSkill = js;
                            break;
                        }
                    }

                    // Add the skill score if found, otherwise 0
                    if (foundSkill != null)
                        jobRow.Add(foundSkill.SkillScore);
                    else
                        jobRow.Add(0.0m);
                }

                matrix.Add(jobRow);
            }

            List<decimal> studentVector = new List<decimal>();
            foreach (var skillName in allSkills)
            {
                StudentSkill? foundSkill = null;

                foreach (var sS in studentSkills)
                {
                    if (sS.SkillName == skillName)
                    {
                        foundSkill = sS;
                        break;
                    }
                }

                if (foundSkill != null)
                    studentVector.Add(foundSkill.SkillScore);
                else
                    studentVector.Add(0.0m);
            }


            List<decimal> similarityScores = new List<decimal>();

            for (int i = 0; i < jobs.Count(); i++)
            {
                var jobVector = matrix[i];
                decimal similarity = JobMatchingService.ComputeCosineSimilarity(studentVector, jobVector);
                similarityScores.Add(similarity);
            }

            // Pair jobs with similarity and sort descending
            List<(JobPost Job, decimal Similarity)> results = new List<(JobPost, decimal)>();
            for (int i = 0; i < jobs.Count(); i++)
            {
                results.Add((jobs[i], similarityScores[i]));
            }

            results.Sort((a, b) => b.Similarity.CompareTo(a.Similarity));

            return View("~/Views/JobMatching/MatchedJobs.cshtml", results);


        }


    }
}
