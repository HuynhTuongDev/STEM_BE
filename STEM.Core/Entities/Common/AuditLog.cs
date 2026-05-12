using STEM.Core.Entities.Common;

namespace STEM.Core.Entities.Common;

public class AuditLog : BaseEntity
{
    public int UserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string OldValues { get; set; } = string.Empty;
    public string NewValues { get; set; } = string.Empty;
}
