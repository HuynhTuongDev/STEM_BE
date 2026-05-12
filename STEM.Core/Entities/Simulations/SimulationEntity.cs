using STEM.Core.Entities.Courses;

namespace STEM.Core.Entities.Simulations;

public class SimulationEntity : BaseEntity
{
    public int LessonId { get; set; }

    public Lesson? Lesson { get; set; }
    public ICollection<SimulationTemplate> SimulationTemplates { get; set; } = new List<SimulationTemplate>();
}
