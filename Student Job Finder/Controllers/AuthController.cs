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
            if(userForRegistration.Password == userForRegistration.PasswordConfirm)
            {
                string sqlCheckUserExists = "SELECT Email FROM JobFinderSchema.Auth WHERE Email= '" +
                    userForRegistration.Email + "'";
                IEnumerable<string> existingUsers = _dapper.LoadData<string>(sqlCheckUserExists);
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
                        [PasswordSalt]) VALUES ('" + userForRegistration.Email +
                    "' , @PasswordHash, @PasswordSalt)";


                    List<SqlParameter> sqlParameters = new List<SqlParameter>();

                    SqlParameter passwordSaltParameter = new SqlParameter("@PasswordSalt", SqlDbType.VarBinary);
                    passwordSaltParameter.Value = passwordSalt;
                    SqlParameter passwordHashParameter = new SqlParameter("@PasswordHash", SqlDbType.VarBinary);
                    passwordHashParameter.Value = passwordHash;

                    sqlParameters.Add(passwordSaltParameter);
                    sqlParameters.Add(passwordHashParameter);

                    if(_dapper.ExecuteSqlWithParameters(sqlAddAuth, sqlParameters))
                    {

                        string sqlAddUser = @"INSERT INTO JobFinderSchema.Users(
                            [FirstName],
                            [LastName],
                            [Email],
                            [Role]
                        ) VALUES (" +
                            "'" + userForRegistration.FirstName +
                            "', '" + userForRegistration.LastName +
                            "', '" + userForRegistration.Email +
                            "', '" + userForRegistration.Role +
                         "')";
                        
                        if(_dapper.ExecuteSql(sqlAddUser))
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
                 [PasswordSalt] FROM JobFinderSchema.Auth WHERE Email = '" +
                 userForLogin.Email + "'";

            UserForLoginConfirmationDto userForConfirmation = _dapper
                .LoadDataSingle<UserForLoginConfirmationDto>(sqlForHashAndSalt);

            byte[] passwordHash = _authHelper.GetPasswordHash(userForLogin.Password, userForConfirmation.PasswordSalt);

            for (int i = 0; i < passwordHash.Length; i++)
            {
                if(passwordHash[i] != userForConfirmation.PasswordHash[i])
                {
                    return StatusCode(401,"Incorrect password");
                }
            }

            string userIdSql = @"
                SELECT UserId FROM JobFinderSchema.Users WHERE Email = '" +
                userForLogin.Email + "'";

            int userId = _dapper.LoadDataSingle<int>(userIdSql);

            string userRoleSql = @"
                SELECT Role FROM JobFinderSchema.Users WHERE Email = '" +
                userForLogin.Email + "'";

            string role = _dapper.LoadDataSingle<string>(userRoleSql);

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

            string userIdSql = "SELECT userId FROM JobFinderSchema.Users WHERE UserId = '"
                + userId + "'";

            int userIdFromDb = _dapper.LoadDataSingle<int>(userIdSql);

            string userRoleSql = @"
                SELECT Role FROM JobFinderSchema.Users WHERE UserId = '" + userId + "'";

            string role = _dapper.LoadDataSingle<string>(userRoleSql);

            return Ok(new Dictionary<string, string> {
                {"token", _authHelper.CreateToken(userIdFromDb, role)}
            });
            
        }

    }
}
