using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Student_Inquiry_Assistance_Portal.Models
{
    public class Payment
    {
        [Key]
        public int PaymentID { get; set; }
        public DateTime PaymentDate { get; set; }
        public int Amount { get; set; }
        public string PaymentMode { get; set; }
        [ForeignKey("Student")]
        public int StudentId { get; set; }

        [ForeignKey("Admission")]
        public int AdmissionID { get; set; }
        public Student? Student { get; set; }
        public Admission? Admission { get; set; }
    }
    
}