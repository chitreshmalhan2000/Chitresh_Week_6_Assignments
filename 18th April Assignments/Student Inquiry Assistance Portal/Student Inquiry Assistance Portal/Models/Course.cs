using System.ComponentModel.DataAnnotations;

namespace Student_Inquiry_Assistance_Portal.Models
{
    public class Course
    {
        [Key]
        public int CourseID { get; set; }

        public string CourseName { get; set; }
        public string Description { get; set; }
        public string Duration { get; set; }
        public int FeesAmount { get; set; }

        // Many-to-Many
        public ICollection<Student>? Students { get; set; }

        // One-to-Many
        public ICollection<Enquiry>? Enquiries { get; set; }
        public ICollection<Admission>? Admissions { get; set; }
        public ICollection<Payment>? Payments { get; set; }
    }
}
