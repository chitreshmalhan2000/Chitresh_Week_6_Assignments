using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Student_Inquiry_Assistance_Portal.Models
{
    public class Admission
    {
        [Key]
        public int AdmissionID { get; set; }

        public DateTime AdmissionDate { get; set; }
        public string Status { get; set; }

        // FK
        public int StudentId { get; set; }
        public int CourseID { get; set; }

        [ForeignKey("StudentId")]
        public Student? Student { get; set; }

        [ForeignKey("CourseID")]
        public Course? Course { get; set; }

        public ICollection<Payment>? Payments { get; set; }
    }
}
