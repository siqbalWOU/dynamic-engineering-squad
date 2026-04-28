using System;
using System.ComponentModel.DataAnnotations;

namespace InfrastructureApp.Models
{
    public class ReportFlag
    {
        public int Id { get; set; }

        public int ReportIssueId { get; set; }

        public string UserId { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Category { get; set; } = string.Empty;

        public bool IsDismissed { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties (optional, but good for EF)
        public virtual ReportIssue? ReportIssue { get; set; }
        public virtual Users? User { get; set; }
    }
}
