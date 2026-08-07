using STEM.Core.Entities.Users;

namespace STEM.Core.Entities.Simulations;

public class LabProgress
{
    public Guid Id { get; set; }
    public Guid LabId { get; set; }
    public int StudentId { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime LastOpenedAt { get; set; }
    public int OpenCount { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int? DurationSeconds { get; set; }

    public Lab? Lab { get; set; }
    public User? Student { get; set; }
}
