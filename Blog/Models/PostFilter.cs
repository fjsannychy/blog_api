namespace Blog.Models
{
    public class PostFilter
    {
        public string SearchPost{ get; set; }
        public int PageNumber { get; set; } = 1; // Default to first page
        public int PageSize { get; set; } = 5;   // Show 5 posts at a time
    }
}
