using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Student_Inquiry_Assistance_Portal.Models
{
    public class Enquiry
    {
        [Key]
        public int EnquiryID { get; set; }

        public DateTime EnquiryDate { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string EnquiryType { get; set; }

        // FK
        public int StudentId { get; set; }
        public int CourseID { get; set; }

        [ForeignKey("StudentId")]
        public Student? Student { get; set; }

        [ForeignKey("CourseID")]
        public Course? Course { get; set; }
    }
}
