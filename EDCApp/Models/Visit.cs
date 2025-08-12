using System;

namespace EDCApp.Models
{
    public class Visit
    {
        public Guid VisitId { get; set; }
        public string? VisitNumber { get; set; }
        public DateTime VisitDate { get; set; }
        public int Status { get; set; }
        public Guid SubjectId { get; set; }
    }
}
