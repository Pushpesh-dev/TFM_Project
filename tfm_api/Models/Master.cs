namespace tfm_web.Models
{
    public class Master
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Email { get; set; }
        public string? Password { get; set; }
        public string? Department { get; set; }
        public DateTime? JoiningDate { get; set; }
        public int RoleId { get; set; }
        public string? PasswordHash { get; set; }
    }
}
