using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Student_Job_Finder.Data;
using Student_Job_Finder.Dtos;
using Student_Job_Finder.Models;
using Student_Job_Finder.Services;
using System.Linq;


namespace Student_Job_Finder.Controllers
{
    [Authorize]
    [Route("[controller]")]
    public class JobPostController : Controller
    {
        private readonly DataContextDapper _dapper;
        private readonly FileService _fileService;
        public JobPostController(IConfiguration config, FileService fileService)
        {
            _dapper = new DataContextDapper(config);
            _fileService = fileService;
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
                [Price],
                [PricePeriod],
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
            string postSql = @"SELECT * 
                       FROM JobFinderSchema.Posts
                       WHERE PostId = " + postId;

            var post = _dapper.LoadDataSingle<JobPost>(postSql);
            if (post == null)
                return NotFound();

            string postSkillsSql = @"SELECT *
                         FROM JobFinderSchema.JobSkills
                         WHERE JobPostId = " + postId;

            var postSkills = _dapper.LoadData<JobSkill>(postSkillsSql);

            string studentSkillsSql = @"SELECT *
                         FROM JobFinderSchema.StudentSkills
                         WHERE StudentId = " + this.User.FindFirst("userId")?.Value;

            var studentSkills = _dapper.LoadData<StudentSkill>(studentSkillsSql);

            string getMultimediaFilesSql = "SELECT ImageUrl, VideoUrl FROM JobFinderSchema.Posts WHERE PostId = " + postId;
            var existingPost = _dapper.LoadDataSingle<JobPost>(getMultimediaFilesSql);
            string finalImageName = existingPost.ImageUrl;
            string finalVideoName = existingPost.VideoUrl;


            var vm = new JobSkillsViewModel
            {
                PostId = post.PostId,
                PostTitle = post.PostTitle,
                PostContent = post.PostContent,
                Price = post.Price,
                PricePeriod = post.PricePeriod,
                PostSkills = postSkills.ToList(),
                StudentSkills = studentSkills.ToList(),
                ImageUrl = finalImageName,
                VideoUrl = finalVideoName

            };


            return View("~/Views/JobPosts/JobPost.cshtml", vm);
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
                return Unauthorized();

            string postSql = @"SELECT * FROM JobFinderSchema.Posts WHERE PostId = " + postId;
            var post = _dapper.LoadDataSingle<JobPost>(postSql);

            string skillsSql = @"
            SELECT JobSkillId, JobPostId, SkillName, SkillScore
            FROM JobFinderSchema.JobSkills
            WHERE JobPostId = " + postId;

            string quizQuestionsSql = @"SELECT * FROM JobFinderSchema.QuizQuestions 
                                WHERE JobPostId = " + postId;
            var quizQuestions = _dapper.LoadData<QuizQuestion>(quizQuestionsSql);

            var skills = _dapper.LoadData<JobSkill>(skillsSql);

            var vm = new JobSkillsViewModel
            {
                PostId = post.PostId,
                PostTitle = post.PostTitle,
                PostContent = post.PostContent,
                Price = post.Price,
                PricePeriod = post.PricePeriod,
                PostSkills = skills.ToList(),
                ExistingQuestions = quizQuestions.ToList()
            };

            return View("~/Views/JobPosts/EditPost.cshtml", vm);
        }


        [HttpPost("AddPost")]
        public IActionResult AddPost(JobPostToAddDto postToAdd)
        {
            if (User.FindFirst("userRole")?.Value != "Recruiter")
                return Unauthorized("Only Recruiters can add job posts.");

            string sql = @"
            INSERT INTO JobFinderSchema.Posts (
                UserId,
                PostTitle,
                PostContent,
                Price,
                PricePeriod,
                PostCreated,
                PostUpdated
            )
            VALUES (
                " + User.FindFirst("userId")?.Value + @",
                '" + postToAdd.PostTitle + @"',
                '" + postToAdd.PostContent + @"',
                " + postToAdd.Price.ToString() + @",
                '" + postToAdd.PricePeriod + @"',
                GETDATE(),
                GETDATE()
            );
            SELECT CAST(SCOPE_IDENTITY() as int);
            ";

            int newPostId = _dapper.LoadDataSingle<int>(sql);

            return RedirectToAction("EditPost", "JobPost", new { postId = newPostId });
        }




        [HttpPost("EditPost")]
        public async Task<IActionResult> EditPost(JobPostToEditDto postToEdit, IFormFile? ImageFile, IFormFile? VideoFile)
        {
            string userId = this.User.FindFirst("userId")?.Value;

            string getMultimediaFilesSql = "SELECT ImageUrl, VideoUrl FROM JobFinderSchema.Posts WHERE PostId = " + postToEdit.PostId;
            var existingPost = _dapper.LoadDataSingle<JobPost>(getMultimediaFilesSql);

            string finalImageName = existingPost.ImageUrl;
            string finalVideoName = existingPost.VideoUrl;

            if (ImageFile != null && ImageFile.Length > 0)
            {
                if (!string.IsNullOrEmpty(existingPost.ImageUrl))
                    _fileService.DeleteFile(existingPost.ImageUrl, "Images");
                finalImageName = await _fileService.SaveFileAsync(ImageFile, "Images");
            }


            if (VideoFile != null && VideoFile.Length > 0)
            { 
                if (!string.IsNullOrEmpty(existingPost.VideoUrl))
                    _fileService.DeleteFile(existingPost.VideoUrl, "Videos");
                finalVideoName = await _fileService.SaveFileAsync(VideoFile, "Videos");
            }

            string imgSqlValue = (finalImageName == null) ? "NULL" : "'" + finalImageName + "'";
            string vidSqlValue = (finalVideoName == null) ? "NULL" : "'" + finalVideoName + "'";

            string sql = @"
            UPDATE JobFinderSchema.Posts
                SET PostContent = '" + postToEdit.PostContent +
                "', PostTitle = '" + postToEdit.PostTitle +
                "', Price = " + postToEdit.Price +
                ", PricePeriod = '" + postToEdit.PricePeriod +
                "', ImageUrl = " + imgSqlValue +
                ", VideoUrl = " + vidSqlValue +
                @", PostUpdated = GETDATE()
                    WHERE PostId = " + postToEdit.PostId.ToString() +
                    " AND UserId = " + userId;

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

        [HttpPost("AddJobSkill")]
        public IActionResult AddJobSkill(JobSkillToAddDto skillToAdd)
        {
            if (User.FindFirst("userRole")?.Value != "Recruiter")
                return Unauthorized();

            decimal skillScore = 0.60m;
            switch (skillToAdd.SkillLevel)
            {
                case "Beginner": skillScore = 0.60m;
                    break;
                case "Intermediate": skillScore = 0.65m;
                    break;
                case "Advanced":
                    skillScore = 0.75m;
                    break;
                case "Expert":
                    skillScore = 0.85m;
                    break;
                default: throw new Exception("Invalid skill level");

            }

            string sql = @"
                INSERT INTO JobFinderSchema.JobSkills
                    (JobPostId, SkillName, SkillScore)
                VALUES (" +
                    skillToAdd.JobPostId + ", '" +
                    skillToAdd.SkillName + "', " +
                    skillScore.ToString() +
                ")";

            if (_dapper.ExecuteSql(sql))
                return RedirectToAction("EditPost", new { postId = skillToAdd.JobPostId });

            throw new Exception("Failed to add job skill");
        }


        [HttpPost("DeleteJobSkill/{jobSkillId}")]
        public IActionResult DeleteJobSkill(int jobSkillId, int postId)
        {
            if (User.FindFirst("userRole")?.Value != "Recruiter")
                return Unauthorized();

            string sql = @"
                DELETE FROM JobFinderSchema.JobSkills
                WHERE JobSkillId = " + jobSkillId +
                " AND JobPostId = " + postId;

            if (_dapper.ExecuteSql(sql))
            {
                return RedirectToAction("EditPost", new { postId });
            }

            throw new Exception("Failed to delete job skill");
        }







    }
}
