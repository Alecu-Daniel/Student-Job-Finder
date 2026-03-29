using Dapper;
using Microsoft.AspNetCore.Mvc;
using Student_Job_Finder.Data;
using Student_Job_Finder.Dtos;
using Student_Job_Finder.Models;
using System.Data;

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
                   WHERE UserId = @UserId";

            DynamicParameters parameters = new DynamicParameters();
            parameters.Add("UserId", userId, DbType.Int32);

            User user = _dapper.LoadDataSingleWithParameters<User>(sql, parameters);
            return user;
        }

        [HttpPut("EditUser")]
        public IActionResult EditUser(User user)
        {
            string sql = @"
                UPDATE JobFinderSchema.Users
                    SET [FirstName] = @FirstName, 
                        [LastName] = @LastName, 
                        [Email] = @Email, 
                        [Role] = @Role
                    WHERE UserId = @UserId";

            DynamicParameters parameters = new DynamicParameters();
            parameters.Add("FirstName", user.FirstName, DbType.String);
            parameters.Add("LastName", user.LastName, DbType.String);
            parameters.Add("Email", user.Email, DbType.String);
            parameters.Add("Role", user.Role, DbType.String);
            parameters.Add("UserId", user.UserId, DbType.Int32);

            if (_dapper.ExecuteSqlWithParameters(sql, parameters))
            {
                return Ok();
            }

            throw new Exception("Failed to Update User");
        }

        [HttpPost("AddUser")]
        public IActionResult AddUser(UserToAddDto user)
        {
            string sql = @"
                INSERT INTO JobFinderSchema.Users (
                    [FirstName],
                    [LastName],
                    [Email],
                    [Role]
                ) VALUES (
                    @FirstName, 
                    @LastName, 
                    @Email, 
                    @Role
                )";

            DynamicParameters parameters = new DynamicParameters();
            parameters.Add("FirstName", user.FirstName, DbType.String);
            parameters.Add("LastName", user.LastName, DbType.String);
            parameters.Add("Email", user.Email, DbType.String);
            parameters.Add("Role", user.Role, DbType.String);

            if (_dapper.ExecuteSqlWithParameters(sql, parameters))
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
                WHERE UserId = @UserId";

            DynamicParameters parameters = new DynamicParameters();
            parameters.Add("UserId", userId, DbType.Int32);

            if (_dapper.ExecuteSqlWithParameters(sql, parameters))
            {
                return Ok();
            }

            throw new Exception("Failed to Delete User");

        }

    }


}
