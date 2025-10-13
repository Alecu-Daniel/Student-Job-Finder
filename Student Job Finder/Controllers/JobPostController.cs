using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Student_Job_Finder.Data;
using Student_Job_Finder.Dtos;
using Student_Job_Finder.Models;

namespace Student_Job_Finder.Controllers
{
    [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class JobPostController : Controller
    {
        private readonly DataContextDapper _dapper;
        public JobPostController(IConfiguration config)
        {
            _dapper = new DataContextDapper(config);
        }

        [HttpGet("Posts")]
        public IEnumerable<JobPost> GetJobPosts()
        {
            string sql = @"SELECT [PostId],
                [UserId],
                [PostTitle],
                [PostContent],
                [PostCreated],
                [PostUpdated]
            FROM JobFinderSchema.Posts";
            return _dapper.LoadData<JobPost>(sql);
        }

        [HttpGet("PostsByUser/{userId}")]
        public IEnumerable<JobPost> GetJobPostsByUser(int userId)
        {
            string sql = @"SELECT [PostId],
                [UserId],
                [PostTitle],
                [PostContent],
                [PostCreated],
                [PostUpdated]
            FROM JobFinderSchema.Posts
                WHERE UserId = " + userId.ToString();
            return _dapper.LoadData<JobPost>(sql);
        }

        [HttpGet("MyPosts")]
        public IEnumerable<JobPost> GetMyJobPosts()
        {
            string sql = @"SELECT [PostId],
                [UserId],
                [PostTitle],
                [PostContent],
                [PostCreated],
                [PostUpdated]
            FROM JobFinderSchema.Posts
                WHERE UserId = " + this.User.FindFirst("userId")?.Value;

            return _dapper.LoadData<JobPost>(sql);
        }

        [HttpGet("PostsBySearch/{searchParam}")]
        public IEnumerable<JobPost> PostBySearch(string searchParam)
        {
            string sql = @"SELECT [PostId],
                [UserId],
                [PostTitle],
                [PostContent],
                [PostCreated],
                [PostUpdated]
            FROM JobFinderSchema.Posts
                WHERE PostTitle LIKE '%" + searchParam +
                "%' OR PostContent LIKE '%" + searchParam + "%'";

            return _dapper.LoadData<JobPost>(sql);
        }

        [HttpGet("PostSingle/{postId}")]
        public JobPost GetJobPosts(int postId)
        {
            string sql = @"SELECT [PostId],
                [UserId],
                [PostTitle],
                [PostContent],
                [PostCreated],
                [PostUpdated]
            FROM JobFinderSchema.Posts
                WHERE PostId = " + postId.ToString();
            return _dapper.LoadDataSingle<JobPost>(sql);
        }

        [HttpPost("JobPost")]
        public IActionResult AddPost(JobPostToAddDto postToAdd)
        {
            string sql = @"
            INSERT INTO JobFinderSchema.Posts(
                [UserId],
                [PostTitle],
                [PostContent],
                [PostCreated],
                [PostUpdated]
            ) VALUES (" + this.User.FindFirst("userId")?.Value
            + ",'" + postToAdd.PostTitle
            + "','" + postToAdd.PostContent
            + "', GETDATE() , GETDATE() )";
            if(_dapper.ExecuteSql(sql))
            {
                return Ok();
            }

            throw new Exception("Failed to create new post!");
        }


        [HttpPut("JobPost")]
        public IActionResult EditPost(JobPostToEditDto postToEdit)
        {
            string sql = @"
            UPDATE JobFinderSchema.Posts
                SET PostContent = '" + postToEdit.PostContent +
                "', PostTitle = '" + postToEdit.PostTitle +
                @"', PostUpdated = GETDATE()
                    WHERE PostId = " + postToEdit.PostId.ToString() +
                    "AND UserId = " + this.User.FindFirst("userId")?.Value;

            if (_dapper.ExecuteSql(sql))
            {
                return Ok();
            }

            throw new Exception("Failed to edit post!");
        }


        [HttpDelete("JobPost/{postId}")]
        public IActionResult DeletePost(int postId)
        {
            string sql = @"DELETE FROM JobFinderSchema.Posts 
                            WHERE PostId = " + postId.ToString()
                            + "AND UserId = " + this.User.FindFirst("userId")?.Value;

            if (_dapper.ExecuteSql(sql))
            {
                return Ok();
            }

            throw new Exception("Failed to delete post!");

        }
    }
}
