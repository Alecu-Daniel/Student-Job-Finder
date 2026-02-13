using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Student_Job_Finder.Data;
using Student_Job_Finder.Dtos;
using Student_Job_Finder.Models;

namespace Student_Job_Finder.Controllers
{
    [Authorize]
    [Route("[controller]")]
    public class JobApplicationController : Controller
    {
        private readonly DataContextDapper _dapper;

        public JobApplicationController(IConfiguration config)
        {
            _dapper = new DataContextDapper(config);
        }

        [HttpPost("Apply")]
        public IActionResult Apply(JobApplicationToAddDto application, string returnUrl)
        {
            if (User.FindFirst("userRole")?.Value != "Student")
            {
                return Unauthorized("Only Students can apply for jobs.");
            }

            string studentId = this.User.FindFirst("userId")?.Value;

            string duplicateSql = @"
                        SELECT COUNT(1) 
                        FROM JobFinderSchema.JobApplications
                        WHERE JobPostId = " + application.JobPostId +
                        " AND StudentId = " + studentId;

            int alreadyApplied = _dapper.LoadDataSingle<int>(duplicateSql);

            if (alreadyApplied > 0)
            {
                return Ok("You already applied to this job");
            }


            string insertSql = @"
                INSERT INTO JobFinderSchema.JobApplications
                (JobPostId, StudentId, Message)
                VALUES ("
                         + application.JobPostId +
                     "," + studentId +
                     ",'" + application.Message +
                     "'" + ")";

            if (!_dapper.ExecuteSql(insertSql))
                throw new Exception("Failed to apply to job");

            return LocalRedirect(returnUrl);
        }


        [HttpGet("Applications/{postId}")]
        public IActionResult ViewApplications(int postId)
        {
            if (this.User.FindFirst("userRole")?.Value != "Recruiter")
                return Unauthorized("Only recruiters can see applications for job posts");

            string applicationsSql = @"
                SELECT * 
                FROM JobFinderSchema.JobApplications
                WHERE JobPostId = " + postId;
            
            IEnumerable<JobApplication> applications = _dapper.LoadData<JobApplication>(applicationsSql);


            List<JobApplicationViewModel> results = new();

            string jobPostSkillsSql = @"
                     SELECT * FROM JobFinderSchema.JobSkills
                     WHERE JobPostId = " + postId;

            var jobPostSkills = _dapper.LoadData<JobSkill>(jobPostSkillsSql);

            foreach (var app in applications)
            {
                string studentInfoSql = @"
                    SELECT FirstName, LastName, Email
                    FROM JobFinderSchema.Users
                    WHERE UserId = " + app.StudentId;

                var student = _dapper.LoadDataSingle<User>(studentInfoSql);

                string studentSkillSql = @"
                     SELECT * FROM JobFinderSchema.StudentSkills
                     WHERE StudentId = " + app.StudentId;

                var skills = _dapper.LoadData<StudentSkill>(studentSkillSql);

                

                var vm = new JobApplicationViewModel()
                {
                    FirstName = student.FirstName,
                    LastName = student.LastName,
                    Email = student.Email,
                    StudentSkills = skills.ToList(),
                    RequiredSkills = jobPostSkills.ToList()
                };

                results.Add(vm);
            }

            return View("~/Views/JobPosts/Applications.cshtml", results.OrderByDescending(x => x.MatchScore).ToList());
        }


    }
}