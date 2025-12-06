using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Student_Job_Finder.Data;
using Student_Job_Finder.Dtos;
using Student_Job_Finder.Models;

namespace Student_Job_Finder.Controllers
{
    [Authorize]
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
        public IActionResult GetMyJobPosts()
        {
            string sql = @"SELECT [PostId],
                [UserId],
                [PostTitle],
                [PostContent],
                [PostCreated],
                [PostUpdated]
            FROM JobFinderSchema.Posts
                WHERE UserId = " + this.User.FindFirst("userId")?.Value;

            var posts = _dapper.LoadData<JobPost>(sql);
            return View("~/Views/JobPosts/MyPosts.cshtml", posts);
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
        public IActionResult GetJobPost(int postId)
        {
            string sql = @"SELECT [PostId],
                [UserId],
                [PostTitle],
                [PostContent],
                [PostCreated],
                [PostUpdated]
            FROM JobFinderSchema.Posts
                WHERE PostId = " + postId.ToString();

            var post = _dapper.LoadDataSingle<JobPost>(sql);

            if (post == null)
                return NotFound();

            return View("~/Views/JobPosts/JobPost.cshtml", post);
        }

        [HttpGet("AddPost")]
        public IActionResult AddPost()
        {
            if (User.FindFirst("userRole")?.Value != "Recruiter")
                return Unauthorized("Only Recruiters can add job posts.");

            return View("~/Views/JobPosts/AddPost.cshtml");
        }

        [HttpGet("EditPost/{postId}")]
        public IActionResult EditPost(int postId)
        {
            if (User.FindFirst("userRole")?.Value != "Recruiter")
                return Unauthorized("Only Recruiters can edit job posts.");

            string sql = @"SELECT * FROM JobFinderSchema.Posts WHERE PostId = " + postId;
            var post = _dapper.LoadDataSingle<JobPost>(sql);

            return View("~/Views/JobPosts/EditPost.cshtml", post);
        }

        [HttpPost("AddPost")]
        public IActionResult AddPost(JobPostToAddDto postToAdd)
        {

            if (this.User.FindFirst("userRole")?.Value == "Recruiter")
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
                    return RedirectToAction("MyPosts", "JobPost");
                }
                throw new Exception("Failed to create new post!");
            }
            throw new Exception("Only Recruiters can add job posts!");
        }


        [HttpPost("EditPost")]
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
                return RedirectToAction("MyPosts", "JobPost");
            }

            throw new Exception("Failed to edit post!");
        }


        [HttpPost("DeletePost/{postId}")]
        public IActionResult DeletePost(int postId)
        {
            string sql = @"DELETE FROM JobFinderSchema.Posts 
                            WHERE PostId = " + postId.ToString()
                            + "AND UserId = " + this.User.FindFirst("userId")?.Value;

            if (_dapper.ExecuteSql(sql))
            {
                return RedirectToAction("MyPosts", "JobPost");
            }

            throw new Exception("Failed to delete post!");

        }
    }
}
