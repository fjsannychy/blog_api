using Blog.Data;
using Blog.Models;
using Dapper;
using Microsoft.AspNetCore.Mvc;
namespace Blog.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
  
    public class PostController : Controller
    {
        private readonly DapperContext _context;

       
        public PostController(DapperContext context)
        {
            _context = context;
        }

        [HttpPost("search")]
        public async Task<IActionResult> GetAll([FromBody] PostFilter filter)
        {
            using var connection = _context.CreateConnection();
            var sql = @"SELECT *
                FROM Posts
                WHERE Title LIKE @SearchText
                ORDER BY Title
                OFFSET @Offset ROWS
                FETCH NEXT @PageSize ROWS ONLY;";
            var parameters = new DynamicParameters();
            int offset = (filter.PageNumber-1) * filter.PageSize;
            parameters.Add("SearchText", $"%{filter.SearchPost}%");
            parameters.Add("Offset", offset);
            parameters.Add("PageSize", filter.PageSize);
            var posts = await connection.QueryAsync<Post>(sql, parameters);

            var countSql = "select count(*) from Posts";
            var count = await connection.QueryFirstAsync<int>(countSql);

            var result = new PostViewModel()
            {
                Posts = posts,
                TotalCount = count,
            };


            return Ok(result);


        }
        // GET BY ID: api/post/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            using var connection = _context.CreateConnection();
            var sql = "SELECT Id, Title, Description FROM Posts WHERE Id = @Id";
            var post = await connection.QueryFirstOrDefaultAsync<Post>(sql, new { Id = id });

            if (post == null) return NotFound(new { message = "Post not found" });
            return Ok(post);
        }
        // CREATE POST: api/post
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Post post)
        {
            if (string.IsNullOrEmpty(post.Title)) return BadRequest("Title is required");

            using var connection = _context.CreateConnection();
            // Using MySQL syntax as per your HeadController example
            var sql = @"INSERT INTO Posts (Title, Description) VALUES (@Title, @Description);
                        SELECT CAST(SCOPE_IDENTITY() AS INT);";

            var id = await connection.ExecuteScalarAsync<int>(sql, post);
            post.Id = id;

            return CreatedAtAction(nameof(GetById), new { id = post.Id }, post);
        }
        // UPDATE POST: api/post/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] Post post)
        {
            using var connection = _context.CreateConnection();
            var sql = "UPDATE Posts SET Title = @Title, Description = @Description WHERE Id = @Id";

            var affectedRows = await connection.ExecuteAsync(sql, new
            {
                Title = post.Title,
                Description = post.Description,
                Id = id
            });

            if (affectedRows == 0) return NotFound();
            return NoContent();
        }
        // DELETE POST: api/post/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            using var connection = _context.CreateConnection();
            var sql = "DELETE FROM Posts WHERE Id = @Id";
            var affectedRows = await connection.ExecuteAsync(sql, new { Id = id });

            if (affectedRows == 0) return NotFound();
            return Ok(new { message = "Post deleted successfully" });
        }
    }
}
