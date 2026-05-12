namespace STEM.Core.Entities.Auditing;

public class AuditLog : BaseEntity
{
    public int UserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
}
