using System;
using System.ComponentModel.DataAnnotations;

namespace EDCApp.Models
{
    public class Subject
    {
        public Guid SubjectId { get; set; }
        
        [Required(ErrorMessage = "Subject Code is required")]
        [Display(Name = "Subject Code")]
        public required string SubjectCode { get; set; }
        
        [Display(Name = "Screening Date")]
        public DateTime? ScreeningDate { get; set; }
        
        [Display(Name = "Enrollment Date")]
        public DateTime? EnrollmentDate { get; set; }
        
        [Required(ErrorMessage = "Subject Status is required")]
        [Display(Name = "Subject Status")]
        public int Status { get; set; }
        
        [Required(ErrorMessage = "Trial is required")]
        [Display(Name = "Trial")]
        public Guid TrialId { get; set; }
    }

    // Enum for Subject Status
    public enum SubjectStatus
    {
        Screening = 2,
        Enrolled = 3,
        Completed = 4,
        Withdrawn = 5
    }
}
