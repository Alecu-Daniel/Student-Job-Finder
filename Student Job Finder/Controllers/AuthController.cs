using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;
using Student_Job_Finder.Data;
using Student_Job_Finder.Dtos;
using Student_Job_Finder.Helpers;
using Student_Job_Finder.Models;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Student_Job_Finder.Controllers
{
    [Authorize]
    [Route("[controller]")]
    public class AuthController : Controller
    {
        private readonly DataContextDapper _dapper;
        private readonly AuthHelper _authHelper;

        public AuthController(IConfiguration config)
        {
            _dapper = new DataContextDapper(config);
            _authHelper = new AuthHelper(config);
        }

        [AllowAnonymous]
        [HttpGet("Register")]
        public IActionResult Register()
        {
            return View();
        }

        [AllowAnonymous]
        [HttpGet("Login")]
        public IActionResult Login()
        {
            return View();
        }

        [AllowAnonymous]
        [HttpPost("Register")]
        public IActionResult Register(UserForRegistrationDto userForRegistration)
        {
            if (userForRegistration.Password == userForRegistration.PasswordConfirm)
            {
                string sqlCheckUserExists = "SELECT Email FROM JobFinderSchema.Auth WHERE Email = @Email";

                DynamicParameters checkUserParameters = new DynamicParameters();
                checkUserParameters.Add("Email", userForRegistration.Email, DbType.String);

                IEnumerable<string> existingUsers = _dapper.LoadDataWithParameters<string>(sqlCheckUserExists, checkUserParameters);

                if (existingUsers.Count() == 0)
                {
                    byte[] passwordSalt = new byte[128 / 8];
                    using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
                    {
                        rng.GetNonZeroBytes(passwordSalt);
                    }

                    byte[] passwordHash = _authHelper.GetPasswordHash(userForRegistration.Password, passwordSalt);

                    string sqlAddAuth = @"
                    INSERT INTO JobFinderSchema.Auth ([Email],
                    [PasswordHash],
                    [PasswordSalt]) VALUES (@Email, @PasswordHash, @PasswordSalt)";

                    DynamicParameters authParameters = new DynamicParameters();
                    authParameters.Add("Email", userForRegistration.Email, DbType.String);
                    authParameters.Add("PasswordHash", passwordHash, DbType.Binary);
                    authParameters.Add("PasswordSalt", passwordSalt, DbType.Binary);

                    if (_dapper.ExecuteSqlWithParameters(sqlAddAuth, authParameters))
                    {
                        string sqlAddUser = @"INSERT INTO JobFinderSchema.Users(
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

                        DynamicParameters userParameters = new DynamicParameters();
                        userParameters.Add("FirstName", userForRegistration.FirstName, DbType.String);
                        userParameters.Add("LastName", userForRegistration.LastName, DbType.String);
                        userParameters.Add("Email", userForRegistration.Email, DbType.String);
                        userParameters.Add("Role", userForRegistration.Role, DbType.String);

                        if (_dapper.ExecuteSqlWithParameters(sqlAddUser, userParameters))
                        {
                            return RedirectToAction("Index", "Home");
                        }
                        throw new Exception("Failed to Add user");
                    }
                    throw new Exception("Failed to Register user");
                }
                throw new Exception("User with this email already exists");
            }
            throw new Exception("Passwords do not match");

        }

        [AllowAnonymous]
        [HttpPost("Login")]
        public IActionResult Login(UserForLoginDto userForLogin)
        {
            string sqlForHashAndSalt = @"SELECT 
            [PasswordHash], 
            [PasswordSalt] FROM JobFinderSchema.Auth WHERE Email = @Email";

            DynamicParameters hashAndSaltParameters = new DynamicParameters();
            hashAndSaltParameters.Add("Email", userForLogin.Email, DbType.String);

            UserForLoginConfirmationDto userForConfirmation = _dapper
                .LoadDataSingleWithParameters<UserForLoginConfirmationDto>(sqlForHashAndSalt, hashAndSaltParameters);

            byte[] passwordHash = _authHelper.GetPasswordHash(userForLogin.Password, userForConfirmation.PasswordSalt);

            for (int i = 0; i < passwordHash.Length; i++)
            {
                if (passwordHash[i] != userForConfirmation.PasswordHash[i])
                {
                    return StatusCode(401, "Incorrect password");
                }
            }

            string userIdSql = @"
                SELECT UserId FROM JobFinderSchema.Users WHERE Email = @Email";

            DynamicParameters userIdParameters = new DynamicParameters();
            userIdParameters.Add("Email", userForLogin.Email, DbType.String);

            int userId = _dapper.LoadDataSingleWithParameters<int>(userIdSql, userIdParameters);

            string userRoleSql = @"
                SELECT Role FROM JobFinderSchema.Users WHERE Email = @Email";

            DynamicParameters userRoleParameters = new DynamicParameters();
            userRoleParameters.Add("Email", userForLogin.Email, DbType.String);

            string role = _dapper.LoadDataSingleWithParameters<string>(userRoleSql, userRoleParameters);

            if (Request.ContentType.Contains("application/json"))
            {
                return Ok(new Dictionary<string, string> {
            {"token", _authHelper.CreateToken(userId, role)}
                });
            }

            var token = _authHelper.CreateToken(userId, role);
            Response.Cookies.Append("jwt", token);
            return RedirectToAction("Index", "Home");
        }

        [Authorize]
        [HttpPost("Logout")]
        public IActionResult Logout()
        {
            Response.Cookies.Delete("jwt");

            return RedirectToAction("Index", "Home");
        }


        [HttpGet("RefreshToken")]
        public IActionResult RefreshToken()
        {
            string userId = User.FindFirst("userId")?.Value + "";

            string userIdSql = "SELECT userId FROM JobFinderSchema.Users WHERE UserId = @UserId";

            DynamicParameters userIdParameters = new DynamicParameters();
            userIdParameters.Add("UserId", userId, DbType.String);

            int userIdFromDb = _dapper.LoadDataSingleWithParameters<int>(userIdSql, userIdParameters);

            string userRoleSql = @"
                SELECT Role FROM JobFinderSchema.Users WHERE UserId = @UserId";

            DynamicParameters userRoleParameters = new DynamicParameters();
            userRoleParameters.Add("UserId", userId, DbType.String);

            string role = _dapper.LoadDataSingleWithParameters<string>(userRoleSql, userRoleParameters);

            return Ok(new Dictionary<string, string> {
                {"token", _authHelper.CreateToken(userIdFromDb, role)}
            });

        }

    }
}
