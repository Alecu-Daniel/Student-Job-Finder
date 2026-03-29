using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Student_Job_Finder.Data;
using Student_Job_Finder.Dtos;
using Student_Job_Finder.Models;
using System.Data;

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
                 WHERE JobPostId = @JobPostId
                 AND StudentId = @StudentId";

            DynamicParameters duplicateParameters = new DynamicParameters();
            duplicateParameters.Add("JobPostId", application.JobPostId, DbType.Int32);
            duplicateParameters.Add("StudentId", studentId, DbType.Int32);

            int alreadyApplied = _dapper.LoadDataSingleWithParameters<int>(duplicateSql, duplicateParameters);

            if (alreadyApplied > 0)
            {
                return Ok("You already applied to this job");
            }


            string insertSql = @"
             INSERT INTO JobFinderSchema.JobApplications
             (JobPostId, StudentId, Message)
             VALUES (@JobPostId, @StudentId, @Message)";

            DynamicParameters insertParameters = new DynamicParameters();
            insertParameters.Add("JobPostId", application.JobPostId, DbType.Int32);
            insertParameters.Add("StudentId", studentId, DbType.Int32);
            insertParameters.Add("Message", application.Message, DbType.String);

            if (!_dapper.ExecuteSqlWithParameters(insertSql, insertParameters))
            {
                throw new Exception("Cant create Application");
            }


            string appIdSql = "SELECT JobApplicationId FROM JobFinderSchema.JobApplications WHERE JobPostId = @JobPostId AND StudentId = @StudentId";

            DynamicParameters appIdParameters = new DynamicParameters();
            appIdParameters.Add("JobPostId", application.JobPostId, DbType.Int32);
            appIdParameters.Add("StudentId", studentId, DbType.Int32);

            int newAppId = _dapper.LoadDataSingleWithParameters<int>(appIdSql, appIdParameters);

            string checkQuizSql = "SELECT COUNT(1) FROM JobFinderSchema.QuizQuestions WHERE JobPostId = @JobPostId";

            DynamicParameters checkQuizParameters = new DynamicParameters();
            checkQuizParameters.Add("JobPostId", application.JobPostId, DbType.Int32);

            int questionCount = _dapper.LoadDataSingleWithParameters<int>(checkQuizSql, checkQuizParameters);

            if (questionCount > 0)
            {
                string createQuizSql = @"
             INSERT INTO JobFinderSchema.Quizzes (JobApplicationId, StudentId, CompletedAt)
             VALUES (@JobApplicationId, @StudentId, NULL)";

                DynamicParameters createQuizParameters = new DynamicParameters();
                createQuizParameters.Add("JobApplicationId", newAppId, DbType.Int32);
                createQuizParameters.Add("StudentId", studentId, DbType.Int32);

                if (!_dapper.ExecuteSqlWithParameters(createQuizSql, createQuizParameters))
                {
                    throw new Exception("Cant create Quiz");
                }

                string quizIdSql = "SELECT QuizId FROM JobFinderSchema.Quizzes WHERE JobApplicationId = @JobApplicationId";

                DynamicParameters quizIdParameters = new DynamicParameters();
                quizIdParameters.Add("JobApplicationId", newAppId, DbType.Int32);

                int quizId = _dapper.LoadDataSingleWithParameters<int>(quizIdSql, quizIdParameters);

                return RedirectToAction("TakeQuiz", "Quiz", new { quizId = quizId });
            }

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
                WHERE JobPostId = @PostId";

            DynamicParameters applicationsParameters = new DynamicParameters();
            applicationsParameters.Add("PostId", postId, DbType.Int32);

            IEnumerable<JobApplication> applications = _dapper.LoadDataWithParameters<JobApplication>(applicationsSql, applicationsParameters);

            List<JobApplicationViewModel> results = new();

            string jobPostSkillsSql = @"
                 SELECT * FROM JobFinderSchema.JobSkills
                 WHERE JobPostId = @PostId";

            DynamicParameters jobPostSkillsParameters = new DynamicParameters();
            jobPostSkillsParameters.Add("PostId", postId, DbType.Int32);

            var jobPostSkills = _dapper.LoadDataWithParameters<JobSkill>(jobPostSkillsSql, jobPostSkillsParameters);

            foreach (var app in applications)
            {
                string studentInfoSql = @"
                    SELECT FirstName, LastName, Email
                    FROM JobFinderSchema.Users
                    WHERE UserId = @StudentId";

                DynamicParameters studentInfoParameters = new DynamicParameters();
                studentInfoParameters.Add("StudentId", app.StudentId, DbType.Int32);

                var student = _dapper.LoadDataSingleWithParameters<User>(studentInfoSql, studentInfoParameters);

                string studentSkillSql = @"
                     SELECT * FROM JobFinderSchema.StudentSkills
                     WHERE StudentId = @StudentId";

                DynamicParameters studentSkillParameters = new DynamicParameters();
                studentSkillParameters.Add("StudentId", app.StudentId, DbType.Int32);

                var skills = _dapper.LoadDataWithParameters<StudentSkill>(studentSkillSql, studentSkillParameters);

                string quizSql = @"SELECT qs.* FROM JobFinderSchema.QuizSkills qs 
                   JOIN JobFinderSchema.Quizzes q ON qs.QuizId = q.QuizId 
                   WHERE q.JobApplicationId = @JobApplicationId";

                DynamicParameters quizParameters = new DynamicParameters();
                quizParameters.Add("JobApplicationId", app.JobApplicationId, DbType.Int32);

                var quizSkills = _dapper.LoadDataWithParameters<QuizSkill>(quizSql, quizParameters);

                var vm = new JobApplicationViewModel()
                {
                    FirstName = student.FirstName,
                    LastName = student.LastName,
                    Email = student.Email,
                    JobApplicationId = app.JobApplicationId,
                    Status = app.Status,
                    StudentSkills = skills.ToList(),
                    RequiredSkills = jobPostSkills.ToList(),
                    QuizResults = quizSkills.ToList()
                };

                results.Add(vm);
            }

            return View("~/Views/JobPosts/Applications.cshtml", results.OrderByDescending(x => x.MatchScore).ToList());
        }


        [HttpPost("AcceptApplication/{jobApplicationId}/{postId}")]
        public IActionResult AcceptApplication(int jobApplicationId, int postId)
        {
            if (this.User.FindFirst("userRole")?.Value != "Recruiter")
                return Unauthorized("Only Recruiters can accept applications");

            string acceptApplicationSql = @"
                UPDATE JobFinderSchema.JobApplications
                SET Status = 'Accepted' 
                WHERE JobApplicationId = @JobApplicationId";

            DynamicParameters parameters = new DynamicParameters();
            parameters.Add("JobApplicationId", jobApplicationId, DbType.Int32);

            var acceptApplication = _dapper.ExecuteSqlWithParameters(acceptApplicationSql, parameters);

            return RedirectToAction("ViewApplications", new { postId = postId });

        }

    }
}