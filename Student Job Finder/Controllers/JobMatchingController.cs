using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Student_Job_Finder.Data;
using Student_Job_Finder.Helpers;
using Student_Job_Finder.Models;
using Student_Job_Finder.Services;
using System.Data;

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

        [HttpGet("MatchJobs")]
        public IActionResult MatchJobsForStudent(string? filterBy = "Match", string? skillFilter = null)
        {
            if (User.FindFirst("userRole")?.Value != "Student")
                return Unauthorized("Only students can use job matching.");

            int userId = int.Parse(User.FindFirst("userId")?.Value ?? "0");

            string studentSql = "SELECT * FROM JobFinderSchema.StudentSkills WHERE StudentId = @StudentId";

            DynamicParameters studentParameters = new DynamicParameters();
            studentParameters.Add("StudentId", userId, DbType.Int32);

            var studentSkills = _dapper.LoadDataWithParameters<StudentSkill>(studentSql, studentParameters);

            string jobsSql = "SELECT * FROM JobFinderSchema.Posts";
            var jobs = _dapper.LoadData<JobPost>(jobsSql).ToList();

            string jobSkillsSql = "SELECT * FROM JobFinderSchema.JobSkills";
            var jobSkills = _dapper.LoadData<JobSkill>(jobSkillsSql);

            //rows = jobs columns = skills
            List<List<decimal>> matrix = new List<List<decimal>>();

            foreach (var job in jobs)
            {
                List<decimal> jobRow = new List<decimal>();

                foreach (var skillName in SkillHelper.allSkills)
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
            foreach (var skillName in SkillHelper.allSkills)
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
                decimal similarity = JobMatchingService.ComputeJobMatchScore(studentVector,jobVector);
                similarityScores.Add(similarity);
            }


            List<JobMatchResultViewModel> results = new();

            for (int i = 0; i < jobs.Count; i++)
            {
                var jobVector = matrix[i];
                var underqualified = new List<UnderqualifiedSkillViewModel>();

                for (int s = 0; s < SkillHelper.allSkills.Count; s++)
                {
                    decimal required = jobVector[s];
                    decimal student = studentVector[s];

                    if (required > 0m && student < required)
                    {
                        underqualified.Add(new UnderqualifiedSkillViewModel
                        {
                            SkillName = SkillHelper.allSkills[s],
                            RequiredLevel = required,
                            StudentLevel = student
                        });
                    }
                }

                results.Add(new JobMatchResultViewModel
                {
                    Job = jobs[i],
                    Similarity = similarityScores[i],
                    JobSkills = jobSkills.Where(js => js.JobPostId == jobs[i].PostId).ToList(),
                    UnderqualifiedSkills = underqualified
                });
            }


            if (!string.IsNullOrEmpty(skillFilter) && filterBy == "Skill")
            {
                var withSkill = results
                    .Where(r => r.JobSkills.Any(js => js.SkillName == skillFilter))
                    .OrderByDescending(r => r.Similarity)
                    .ThenByDescending(r =>
                        JobMatchingService.NormalizePrice(r.Job.Price, r.Job.PricePeriod));

                var withoutSkill = results
                    .Where(r => !r.JobSkills.Any(js => js.SkillName == skillFilter))
                    .OrderByDescending(r => r.Similarity)
                    .ThenByDescending(r =>
                        JobMatchingService.NormalizePrice(r.Job.Price, r.Job.PricePeriod));

                results = withSkill
                    .Concat(withoutSkill)
                    .ToList();
            }
            else if (filterBy == "Pay")
            {
                results = results
                    .OrderByDescending(r =>
                        JobMatchingService.NormalizePrice(r.Job.Price, r.Job.PricePeriod))
                    .ThenByDescending(r => r.Similarity)
                    .ToList();
            }
            else // Match (default)
            {
                results = results
                    .OrderByDescending(r => r.Similarity)
                    .ThenByDescending(r =>
                        JobMatchingService.NormalizePrice(r.Job.Price, r.Job.PricePeriod))
                    .ToList();
            }

            return View("~/Views/JobMatching/MatchedJobs.cshtml", results);


        }


    }
}
