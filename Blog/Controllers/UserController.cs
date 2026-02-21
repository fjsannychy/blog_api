using Dapper;
using Microsoft.AspNetCore.Mvc;
using Blog.Data;  
using Blog.Models; 

namespace Blog.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly DapperContext _context;

        public UsersController(DapperContext context)
        {
            _context = context;
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