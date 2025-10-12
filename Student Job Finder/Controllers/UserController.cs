using Microsoft.AspNetCore.Mvc;
using Student_Job_Finder.Data;
using Student_Job_Finder.Dtos;
using Student_Job_Finder.Models;

namespace Student_Job_Finder.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class UserController : Controller
    {

        DataContextDapper _dapper;
        public UserController(IConfiguration config)
        {
            _dapper = new DataContextDapper(config);
        }

        [HttpGet("GetSingleUser/{userId}")]
        public User GetSingleUser(int userId)
        {
            string sql = @"
                  SELECT [UserId],
                        [FirstName],
                        [LastName],
                        [Email],
                        [Role]
                  FROM JobFinderSchema.Users
                       WHERE UserId = " + userId.ToString();
            User user = _dapper.LoadDataSingle<User>(sql);
            return user;
        }

        [HttpPut("EditUser")]
        public IActionResult EditUser(User user)
        {
            string sql = @"
            UPDATE JobFinderSchema.Users
                SET [Firstname] = '" + user.FirstName + 
                "', [LastName] = '" + user.LastName + 
                "', [Email] = '" + user.Email +
                "', [Role] = '" + user.Role + "'" +
                "WHERE UserId= " + user.UserId;
            if (_dapper.ExecuteSql(sql))
            {
                return Ok();
            }

            throw new Exception("Failed to Update User");
        }

        [HttpPost("AddUser")]
        public IActionResult AddUser(UserToAddDto user)
        {
            string sql = @"INSERT INTO JobFinderSchema.Users(
                [FirstName],
                [LastName],
                [Email],
                [Role]
            )VALUES(" +
                "'" + user.FirstName + 
                "','" + user.LastName + 
                "','" + user.Email +
                "','" + user.Role + 
           "')";

            if (_dapper.ExecuteSql(sql))
            {
                return Ok();
            }

            throw new Exception("Failed to Add User");
        }

        [HttpDelete("DeleteUser/{userId}")]
        public IActionResult DeleteUser(int userId)
        {
            string sql = @"
                DELETE FROM JobFinderSchema.Users
                    WHERE UserId = " + userId.ToString();

            if (_dapper.ExecuteSql(sql))
            {
                return Ok();
            }

            throw new Exception("Failed to Delete User");

        }

    }


}
