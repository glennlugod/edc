using System;
using System.ComponentModel.DataAnnotations;

namespace EDCApp.Models
{
    public class Trial
    {
        public Guid Id { get; set; }

        [Required(ErrorMessage = "Trial Name is required")]
        [Display(Name = "Trial Name")]
        public required string TrialName { get; set; }

        [Display(Name = "Sponsor")]
        public string? Sponsor { get; set; }

        [Display(Name = "Start Date")]
        [DataType(DataType.Date)]
        public DateTime? StartDate { get; set; }

        [Display(Name = "End Date")]
        [DataType(DataType.Date)]
        public DateTime? EndDate { get; set; }
    }
}
