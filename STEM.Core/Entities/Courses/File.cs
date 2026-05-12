namespace STEM.Core.Entities.Courses; public class File : BaseEntity { public int MaterialId { get; set; } public string Url { get; set; } = string.Empty; public Material? Material { get; set; } }
