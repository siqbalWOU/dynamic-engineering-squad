namespace InfrastructureApp.Models
{
    public class AuditLog
    {
        public int Id { get; set; }

        public string? AspNetUserId { get; set; }

        public string? UserName { get; set; }

        public string? Email { get; set; }

        public string? Role { get; set; }

        public DateTime TimestampUtc { get; set; }

        public string Action { get; set; } = string.Empty;
    }
}
