namespace Blog.Models
{
    public class UsersModel
    {
        public int Userid { get; set; } 
        public string Username { get; set; } = string.Empty;
        public string Fullname { get; set; } = string.Empty; 
        public string Password { get; set; } = string.Empty;
    }
}
