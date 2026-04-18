using System.ComponentModel.DataAnnotations;

namespace Student_Inquiry_Assistance_Portal.Models
{
    public class User
    {
        [Key]
        public long UserId { get; set; }

        [Required]
        public string Email { get; set; }

        public string Password { get; set; }
        public string Username { get; set; }
        public string MobileNumber { get; set; }
        public string UserRole { get; set; }

        // One-to-One with Student
        public Student? Student { get; set; }
    }
}
