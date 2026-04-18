using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Student_Inquiry_Assistance_Portal.Models
{
    public class Student
    {
        [Key]
        public int StudentId { get; set; }

        public string StudentName { get; set; }
        public string StudentEmailId { get; set; }

        // FK
        public long UserId { get; set; }

        [ForeignKey("UserId")]
        public User User { get; set; }

        // Many-to-Many
        public ICollection<Course> Courses { get; set; }

        // One-to-Many
        public ICollection<Enquiry> Enquiries { get; set; }
        public ICollection<Admission> Admissions { get; set; }
    }
}
