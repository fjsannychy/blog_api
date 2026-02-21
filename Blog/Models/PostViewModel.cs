namespace Blog.Models
{
    public class PostViewModel
    {
        // This holds the 5 posts for the current page
        public IEnumerable<Post> Posts { get; set; } = new List<Post>();

        // This holds the total number of posts matching the search (e.g., 50)
        public int TotalCount { get; set; }
    }
}
