using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InfrastructureApp.Models
{
    public class ModerationActionLog
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(450)]
        public string ModeratorId { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Action { get; set; } = string.Empty; // "Dismissed", "Removed"

        public int? ReportIssueId { get; set; }

        [Required]
        public string TargetContentSnapshot { get; set; } = string.Empty;

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("ModeratorId")]
        public virtual Users? Moderator { get; set; }
    }
}
