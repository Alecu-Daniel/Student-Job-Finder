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
        public IEnumerable<StudentSkill> MySkills()
        {
            string sql = @"
                SELECT [StudentSkillId],
                    [StudentId],
                    [SkillName],
                    [SkillScore]
                    FROM JobFinderSchema.StudentSkills
                WHERE StudentId = " + this.User.FindFirst("userId").Value;
            return _dapper.LoadData<StudentSkill>(sql);
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
