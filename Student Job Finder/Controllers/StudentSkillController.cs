using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Student_Job_Finder.Data;
using Student_Job_Finder.Dtos;
using Student_Job_Finder.Models;
using Student_Job_Finder.Helpers;
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
                    WHERE StudentId = " + studentId.ToString();
            return _dapper.LoadData<StudentSkill>(sql);
        }


        [HttpGet("MySkills")]
        public  IActionResult MySkills()
        {
            string studentSql = @"
                SELECT [StudentSkillId],
                    [StudentId],
                    [SkillName],
                    [SkillScore]
                    FROM JobFinderSchema.StudentSkills
                WHERE StudentId = " + this.User.FindFirst("userId")?.Value;

            var studentSkills = _dapper.LoadData<StudentSkill>(studentSql);

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
                    if( skill.SkillName == job.SkillName)
                    {
                        SkillLevel jobLevel = SkillHelper.GetSkillLevel(job.SkillScore);

                        if(jobLevel == studentLevel + 1)
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
                    else
                    {
                        continue;
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
            if (skills == null || skills.Count == 0)
                throw new Exception("No skills provided!");

            foreach(var skill in skills)
            {
                string sql = @"
                INSERT INTO JobFinderSchema.StudentSkills
                ([StudentId], [SkillName], [SkillScore])
                VALUES (" + this.User.FindFirst("userId")?.Value
                + ",'" + skill.SkillName
                + "'," + skill.SkillScore.ToString()
                + ")";

                if (!_dapper.ExecuteSql(sql))
                {
                    throw new Exception("Failed to create new skill!");
                }


            }

            return Ok();
        }

        [HttpPut("EditSkills")]
        public IActionResult EditSkills([FromBody] List<StudentSkillToAddDto> skills)
        {

            string sqlDelete = @"
                DELETE FROM JobFinderSchema.StudentSkills
                    WHERE StudentId = " + this.User.FindFirst("userId")?.Value;

            if (!_dapper.ExecuteSql(sqlDelete))
            {
                throw new Exception("Failed to delete skill!");
            }

            if (skills == null || skills.Count == 0)
                throw new Exception("No skills provided!");

            foreach (var skill in skills)
            {
                string sqlEdit = @"
                INSERT INTO JobFinderSchema.StudentSkills
                ([StudentId], [SkillName], [SkillScore])
                VALUES (" + this.User.FindFirst("userId")?.Value
                + ",'" + skill.SkillName
                + "'," + skill.SkillScore.ToString()
                + ")";

                if (!_dapper.ExecuteSql(sqlEdit))
                {
                    throw new Exception("Failed to create new skill!");
                }


            }

            return Ok();
        }

        [HttpDelete("DeleteSkills")]
        public IActionResult DeleteSkills()
        {
            string sqlDelete = @"
                DELETE FROM JobFinderSchema.StudentSkills
                    WHERE StudentId = " + this.User.FindFirst("userId")?.Value;

            if (_dapper.ExecuteSql(sqlDelete))
            {
                return Ok();
            }

            throw new Exception("Failed to delete skill!");
        }

    }
}
