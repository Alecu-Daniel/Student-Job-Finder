using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Student_Job_Finder.Data;
using Student_Job_Finder.Dtos;
using Student_Job_Finder.Helpers;
using Student_Job_Finder.Models;
using System.Data;
using System.Reflection.Metadata.Ecma335;

namespace Student_Job_Finder.Controllers
{
    [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class StudentSkillController : Controller
    {

        private readonly DataContextDapper _dapper;
        public StudentSkillController(IConfiguration config)
        {
            _dapper = new DataContextDapper(config);
        }

        [HttpGet("Skills")]
        public IEnumerable<StudentSkill> GetSkills()
        {
            string sql = @"SELECT [StudentSkillId],
                    [StudentId],
                    [SkillName],
                    [SkillScore]
                    FROM JobFinderSchema.StudentSkills";
            return _dapper.LoadData<StudentSkill>(sql);
        }

        [HttpGet("SkillsByStudent/{studentId}")]
        public IEnumerable<StudentSkill> GetSkillsByUser(int studentId)
        {
            string sql = @"SELECT [StudentSkillId],
                [StudentId],
                [SkillName],
                [SkillScore]
                FROM JobFinderSchema.StudentSkills
                WHERE StudentId = @StudentId";

            DynamicParameters parameters = new DynamicParameters();
            parameters.Add("StudentId", studentId, DbType.Int32);

            return _dapper.LoadDataWithParameters<StudentSkill>(sql, parameters);
        }


        [HttpGet("MySkills")]
        public  IActionResult MySkills()
        {
            string userId = this.User.FindFirst("userId")?.Value;

            string studentSql = @"
                SELECT [StudentSkillId],
                    [StudentId],
                    [SkillName],
                    [SkillScore]
                    FROM JobFinderSchema.StudentSkills
                WHERE StudentId = @StudentId";

            DynamicParameters studentParams = new DynamicParameters();
            studentParams.Add("StudentId", userId, DbType.String);

            var studentSkills = _dapper.LoadDataWithParameters<StudentSkill>(studentSql, studentParams);

            string jobSql = @"
                SELECT [JobSkillId],
                       [JobPostId],
                       [SkillName],
                       [SkillScore]
                FROM JobFinderSchema.JobSkills";

            var jobSkills = _dapper.LoadData<StudentSkill>(jobSql);

            Dictionary<string, int> potentialJobsWithImprovement = new Dictionary<string, int>();

            foreach (var skill in studentSkills)
            {
                SkillLevel studentLevel = SkillHelper.GetSkillLevel(skill.SkillScore);

                foreach (var job in jobSkills)
                {
                    if (skill.SkillName == job.SkillName)
                    {
                        SkillLevel jobLevel = SkillHelper.GetSkillLevel(job.SkillScore);

                        if (jobLevel == studentLevel + 1)
                        {
                            if (potentialJobsWithImprovement.ContainsKey(skill.SkillName))
                            {
                                potentialJobsWithImprovement[skill.SkillName]++;
                            }
                            else
                            {
                                potentialJobsWithImprovement.Add(skill.SkillName, 1);
                            }
                        }
                    }
                }
            }

            var vm = new StudentSkillsViewModel
            {
                StudentSkills = studentSkills.ToList(),
                PotentialJobs = potentialJobsWithImprovement
            };

            return View("~/Views/Profile/Profile.cshtml", vm);
        }


        [HttpPost("AddSkills")]
        public IActionResult AddSkills([FromBody] List<StudentSkillToAddDto> skills)
        {
            if (skills == null || !skills.Any())
                throw new Exception("No skills provided!");

            string userId = this.User.FindFirst("userId")?.Value;

            foreach (var skill in skills)
            {
                string sql = @"
                    INSERT INTO JobFinderSchema.StudentSkills
                    ([StudentId], [SkillName], [SkillScore])
                    VALUES (@UserId, @SkillName, @SkillScore)";

                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("UserId", userId, DbType.String);
                parameters.Add("SkillName", skill.SkillName, DbType.String);
                parameters.Add("SkillScore", skill.SkillScore, DbType.Decimal);

                if (!_dapper.ExecuteSqlWithParameters(sql, parameters))
                {
                    throw new Exception($"Failed to create new skill: {skill.SkillName}!");
                }
            }

            return Ok();
        }

        [HttpPut("EditSkills")]
        public IActionResult EditSkills([FromBody] List<StudentSkillToAddDto> skills)
        {

            string userId = this.User.FindFirst("userId")?.Value;

            string sqlDelete = @"
                DELETE FROM JobFinderSchema.StudentSkills
                WHERE StudentId = @UserId";

            DynamicParameters deleteParams = new DynamicParameters();
            deleteParams.Add("UserId", userId, DbType.String);

            if (!_dapper.ExecuteSqlWithParameters(sqlDelete, deleteParams))
            {
                throw new Exception("Failed to delete existing skills!");
            }

            if (skills == null || skills.Count == 0)
                throw new Exception("No skills provided!");

            foreach (var skill in skills)
            {
                string sqlEdit = @"
                    INSERT INTO JobFinderSchema.StudentSkills
                    ([StudentId], [SkillName], [SkillScore])
                    VALUES (@UserId, @SkillName, @SkillScore)";

                DynamicParameters insertParams = new DynamicParameters();
                insertParams.Add("UserId", userId, DbType.String);
                insertParams.Add("SkillName", skill.SkillName, DbType.String);
                insertParams.Add("SkillScore", skill.SkillScore, DbType.Decimal);

                if (!_dapper.ExecuteSqlWithParameters(sqlEdit, insertParams))
                {
                    throw new Exception($"Failed to create skill: {skill.SkillName}!");
                }
            }

            return Ok();
        }

        [HttpDelete("DeleteSkills")]
        public IActionResult DeleteSkills()
        {
            string userId = this.User.FindFirst("userId")?.Value;

            string sqlDelete = @"
                DELETE FROM JobFinderSchema.StudentSkills
                WHERE StudentId = @UserId";

            DynamicParameters parameters = new DynamicParameters();
            parameters.Add("UserId", userId, DbType.String);

            if (_dapper.ExecuteSqlWithParameters(sqlDelete, parameters))
            {
                return Ok();
            }

            throw new Exception("Failed to delete skill!");
        }

    }
}
