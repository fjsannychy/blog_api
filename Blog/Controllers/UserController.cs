using Azure.Core;
using Blog.Data;  
using Blog.Models; 
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Blog.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly DapperContext _context;
        private readonly IConfiguration _configuration;

        public UsersController(DapperContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }


        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> LoginAsync(LoginRequest request)
        {
            using var connection = _context.CreateConnection();
            var sql = "SELECT userid, username, fullname, password FROM users WHERE username = @Username and password = @Password";
            var user = await connection.QueryFirstOrDefaultAsync<UsersModel>(sql, 
                                                 new {
                                                     Username = request.Username,
                                                     Password = request.Password,
                                                 });

            if (user == null)
                return Unauthorized("Invalid credentials");

            var accessToken = GenerateAccessToken(user);
            var refreshToken = GenerateRefreshToken(user.Userid);


            var insertRefreshTokenSql = @"INSERT INTO RefreshToken (Token, UserId, ExpiresAt,IsRevoked) 
                                           VALUES (@Token, @UserId, @ExpiresAt, @IsRevoked);";

            await connection.ExecuteScalarAsync(insertRefreshTokenSql, refreshToken);

            return Ok(new
            {
                accessToken,
                refreshToken = refreshToken.Token
            });
        }

        private string GenerateAccessToken(UsersModel user)
        {
            var jwt = _configuration.GetSection("Jwt");

            var claims = new[]
            {
            new Claim(ClaimTypes.Name, user.Username),
            //new Claim(ClaimTypes.Role, user.Role),
            new Claim("UserId", user.Userid.ToString())
            };

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwt["Key"]!)
            );

            var token = new JwtSecurityToken(
                issuer: jwt["Issuer"],
                audience: jwt["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(
                    int.Parse(jwt["AccessTokenExpiryMinutes"]!)
                ),
                signingCredentials: new SigningCredentials(
                    key, SecurityAlgorithms.HmacSha256)
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private RefreshToken GenerateRefreshToken(int userId)
        {
            var jwt = _configuration.GetSection("Jwt");

            var randomBytes = RandomNumberGenerator.GetBytes(64);

            return new RefreshToken
            {
                UserId = userId,
                Token = Convert.ToBase64String(randomBytes),
                ExpiresAt = DateTime.UtcNow.AddDays(
                    int.Parse(jwt["RefreshTokenExpiryDays"]!)
                ),
                IsRevoked = false
            };
        }

        [HttpPost("refresh")]
        [AllowAnonymous]
        public async Task<IActionResult> RefreshAsync(TokenRequest request)
        {

            using var connection = _context.CreateConnection();
            var sql = "SELECT * FROM RefreshToken WHERE token = @Token and IsRevoked = 0 and  ExpiresAt > @CurrentUTC ";
            var storedToken = await connection.QueryFirstOrDefaultAsync<RefreshToken>(sql,
                                                 new
                                                 {
                                                     Token = request.RefreshToken,
                                                     CurrentUTC = DateTime.UtcNow
                                                 });

            if (storedToken == null)
                return Unauthorized("Invalid refresh token");

            var userSql = "SELECT userid, username, fullname, password FROM users WHERE userid = @Userid";
            var user = await connection.QueryFirstOrDefaultAsync<UsersModel>(userSql, new { Userid = storedToken.UserId });
            if (user == null)
                return Unauthorized();

            var deleteRefreshTokenSql = "DELETE FROM RefreshToken WHERE Token = @Token";
            var affectedRows = await connection.ExecuteAsync(deleteRefreshTokenSql, new { Token = storedToken.Token });

            var newRefreshToken = GenerateRefreshToken(user.Userid);

            var insertRefreshTokenSql = @"INSERT INTO RefreshToken (Token, UserId, ExpiresAt,IsRevoked) 
                                           VALUES (@Token, @UserId, @ExpiresAt, @IsRevoked);
                                           SELECT CAST(SCOPE_IDENTITY() AS NVARCHAR(MAX));";

            await connection.ExecuteScalarAsync(insertRefreshTokenSql, newRefreshToken);

            var newAccessToken = GenerateAccessToken(user);

            return Ok(new
            {
                accessToken = newAccessToken,
                refreshToken = newRefreshToken.Token
            });
        }

        [HttpPost("logout")]
        [AllowAnonymous]
        public async Task<IActionResult> Logout(TokenRequest request)
        {
            using var connection = _context.CreateConnection();
            var deleteRefreshTokenSql = "DELETE FROM RefreshToken WHERE Token = @Token";
            var affectedRows = await connection.ExecuteAsync(deleteRefreshTokenSql, new { Token = request.RefreshToken });

            return Ok();
        }

        // 1. GET ALL USERS: api/users
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            using var connection = _context.CreateConnection();
            var sql = "SELECT userid, username, fullname, password FROM users";
            var users = await connection.QueryAsync<UsersModel>(sql);
            return Ok(users);
        }

        // 2. GET BY ID: api/users/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            using var connection = _context.CreateConnection();
            var sql = "SELECT userid, username, fullname, password FROM users WHERE userid = @Userid";
            var user = await connection.QueryFirstOrDefaultAsync<UsersModel>(sql, new { Userid = id });

            if (user == null) return NotFound(new { message = "User not found" });
            return Ok(user);
        }

        // 3. CREATE USER: api/users
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] UsersModel user)
        {
            if (string.IsNullOrEmpty(user.Username)) return BadRequest("Username is required");

            using var connection = _context.CreateConnection();
            
            var sql = @"INSERT INTO users (username, fullname, password) 
                        VALUES (@Username, @Fullname, @Password);
                        SELECT CAST(SCOPE_IDENTITY() AS INT);";

            var id = await connection.ExecuteScalarAsync<int>(sql, user);
            user.Userid = id;

            return CreatedAtAction(nameof(GetById), new { id = user.Userid }, user);
        }

        // 4. UPDATE USER: api/users/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UsersModel user)
        {
            using var connection = _context.CreateConnection();
            var sql = @"UPDATE users 
                        SET username = @Username, fullname = @Fullname, password = @Password 
                        WHERE userid = @Userid";

            var affectedRows = await connection.ExecuteAsync(sql, new
            {
                Username = user.Username,
                Fullname = user.Fullname,
                Password = user.Password,
                Userid = id
            });

            if (affectedRows == 0) return NotFound();
            return Ok(new { message = "User updated successfully" });
        }

        // 5. DELETE USER: api/users/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            using var connection = _context.CreateConnection();
            var sql = "DELETE FROM users WHERE userid = @Userid";
            var affectedRows = await connection.ExecuteAsync(sql, new { Userid = id });

            if (affectedRows == 0) return NotFound();
            return Ok(new { message = "User deleted successfully" });
        }
    }
}