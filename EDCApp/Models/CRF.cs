using System;

namespace EDCApp.Models
{
    public class CRF
    {
        public Guid Id { get; set; }
        public required string CRFTitle { get; set; }
        public int FormType { get; set; }
        public DateTime? CompletedDate { get; set; }
        public Guid? VerifiedById { get; set; }
        public Guid? VisitId { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime ModifiedOn { get; set; }
    }
}
