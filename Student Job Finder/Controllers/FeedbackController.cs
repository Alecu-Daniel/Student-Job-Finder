using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Student_Job_Finder.Data;
using Student_Job_Finder.Models;
using System.Data;

namespace Student_Job_Finder.Controllers
{
    [Authorize]
    [Route("[controller]")]
    public class FeedbackController : Controller
    {
        private readonly DataContextDapper _dapper;

        public FeedbackController(IConfiguration config)
        {
            _dapper = new DataContextDapper(config);
        }



        [HttpGet("MyAcceptedJobs")]
        public IActionResult MyAcceptedJobs()
        {
            int studentId = int.Parse(this.User.FindFirst("userId")?.Value);

            string sql = @"
                    SELECT ja.JobApplicationId, ja.JobPostId, jp.PostTitle, u.FirstName AS RecruiterName, ja.Status
                    FROM JobFinderSchema.JobApplications AS ja
                    JOIN JobFinderSchema.Posts AS jp ON ja.JobPostId = jp.PostId
                    JOIN JobFinderSchema.Users AS u ON jp.UserId = u.UserId
                    WHERE ja.StudentId = @StudentId AND ja.Status = 'Accepted'";

            DynamicParameters parameters = new DynamicParameters();

            parameters.Add("StudentId", studentId, DbType.Int32);

            var acceptedJobs = _dapper.LoadDataWithParameters<AcceptedJobViewModel>(sql,parameters);

            return View(acceptedJobs);

        }

        [HttpGet("SubmitFeedback/{appId}")]
        public IActionResult SubmitFeedback(int appId)
        {
            string sql = @"
            SELECT ja.JobApplicationId, ja.JobPostId, jp.PostTitle, u.FirstName AS RecruiterName, jp.UserId AS RecruiterId
            FROM JobFinderSchema.JobApplications ja
            JOIN JobFinderSchema.Posts jp ON ja.JobPostId = jp.PostId
            JOIN JobFinderSchema.Users u ON jp.UserId = u.UserId
            WHERE ja.JobApplicationId = @AppId";

            DynamicParameters parameters = new DynamicParameters();

            parameters.Add("AppId", appId, DbType.Int32);

            var info = _dapper.LoadDataSingleWithParameters<AcceptedJobViewModel>(sql,parameters);

            return View(info);
        }


        [HttpPost("PostFeedback")]
        public IActionResult PostFeedback(Feedback feedback)
        {
            feedback.StudentId = int.Parse(this.User.FindFirst("userId")?.Value);
            feedback.CreatedAt = DateTime.Now;

            string sql = @"
            INSERT INTO JobFinderSchema.Feedback (JobApplicationId, JobPostId, StudentId, RecruiterId, Rating, Comment, CreatedAt)
            VALUES (@JobApplicationId, @JobPostId, @StudentId, @RecruiterId, @Rating, @Comment, @CreatedAt)";

            DynamicParameters parameters = new DynamicParameters();

            parameters.Add("JobApplicationId", feedback.JobApplicationId, DbType.Int32);
            parameters.Add("JobPostId", feedback.JobPostId, DbType.Int32);
            parameters.Add("StudentId", feedback.StudentId, DbType.Int32);
            parameters.Add("RecruiterId", feedback.RecruiterId, DbType.Int32);
            parameters.Add("Rating", feedback.Rating, DbType.Int32);
            parameters.Add("Comment", feedback.Comment, DbType.String);
            parameters.Add("CreatedAt", feedback.CreatedAt, DbType.DateTime);

            _dapper.ExecuteSqlWithParameters(sql, parameters);

            return RedirectToAction("MyAcceptedJobs");
        }

        [HttpGet("RecruiterReviews/{postId}")]
        public IActionResult RecruiterReviews(int postId)
        {
            string recruiterLookupSql = "SELECT UserId FROM JobFinderSchema.Posts WHERE PostId = @PostId";

            DynamicParameters lookupParams = new DynamicParameters();
            lookupParams.Add("PostId", postId, DbType.Int32);

            int recruiterId = _dapper.LoadDataSingleWithParameters<int>(recruiterLookupSql, lookupParams);

            string nameSql = "SELECT FirstName + ' ' + LastName FROM JobFinderSchema.Users WHERE UserId = @RecruiterId";

            DynamicParameters recruiterParams = new DynamicParameters();
            recruiterParams.Add("RecruiterId", recruiterId, DbType.Int32);

            string recruiterName = _dapper.LoadDataSingleWithParameters<string>(nameSql, recruiterParams);

            string feedbackSql = @"
                SELECT f.*, p.PostTitle 
                FROM JobFinderSchema.Feedback AS f
                JOIN JobFinderSchema.Posts AS p ON f.JobPostId = p.PostId
                WHERE f.RecruiterId = @RecruiterId
                ORDER BY f.CreatedAt DESC";

            // Reusing your existing recruiterParams
            var reviews = _dapper.LoadDataWithParameters<Feedback>(feedbackSql, recruiterParams);

            ViewBag.RecruiterName = recruiterName;
            return View(reviews);
        }


    }
}
