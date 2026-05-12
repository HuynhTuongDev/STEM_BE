namespace STEM.Core.Entities.Courses; public class Module : BaseEntity { public int CourseId { get; set; } public string Title { get; set; } = string.Empty; public Course? Course { get; set; } }
