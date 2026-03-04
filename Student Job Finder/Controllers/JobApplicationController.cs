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
           
            if(!_dapper.ExecuteSql(insertSql))
           {
                throw new Exception("Cant create Application");
           }



            string appIdSql = "SELECT JobApplicationId FROM JobFinderSchema.JobApplications WHERE JobPostId = " +
                  application.JobPostId + " AND StudentId = " + studentId;

            int newAppId = _dapper.LoadDataSingle<int>(appIdSql);

            string checkQuizSql = "SELECT COUNT(1) FROM JobFinderSchema.QuizQuestions WHERE JobPostId = " + application.JobPostId;
            int questionCount = _dapper.LoadDataSingle<int>(checkQuizSql);

            if (questionCount > 0)
            {
                string createQuizSql = $@"
                    INSERT INTO JobFinderSchema.Quizzes (JobApplicationId, StudentId, CompletedAt)
                    VALUES ({newAppId}, {studentId}, NULL)";

                if (!_dapper.ExecuteSql(createQuizSql))
                {
                    throw new Exception("Cant create Quiz");
                }

                string quizIdSql = "SELECT QuizId FROM JobFinderSchema.Quizzes WHERE JobApplicationId = " + newAppId;

                int quizId = _dapper.LoadDataSingle<int>(quizIdSql);

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

                string quizSql = @"SELECT qs.* FROM JobFinderSchema.QuizSkills qs 
                           JOIN JobFinderSchema.Quizzes q ON qs.QuizId = q.QuizId 
                           WHERE q.JobApplicationId = " + app.JobApplicationId;

                var quizSkills = _dapper.LoadData<QuizSkill>(quizSql);



                var vm = new JobApplicationViewModel()
                {
                    FirstName = student.FirstName,
                    LastName = student.LastName,
                    Email = student.Email,
                    StudentSkills = skills.ToList(),
                    RequiredSkills = jobPostSkills.ToList(),
                    QuizResults = quizSkills.ToList()
                };

                results.Add(vm);
            }

            return View("~/Views/JobPosts/Applications.cshtml", results.OrderByDescending(x => x.MatchScore).ToList());
        }


    }
}