using System;

namespace EDCApp.Models
{
    public class CRFItem
    {
        public Guid Id { get; set; }
        public required string FieldName { get; set; }
        public string? FieldValue { get; set; }
        public string? Units { get; set; }
        public int ItemStatus { get; set; }
        public Guid? CRFId { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime ModifiedOn { get; set; }
    }
}
