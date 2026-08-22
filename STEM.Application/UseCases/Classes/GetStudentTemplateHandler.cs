using STEM.Core.Repository;
using STEM.Core.Entities.Classes;

namespace STEM.Application.UseCases.Classes;

public class GetStudentTemplateHandler
{
    private readonly IClassRepository _classRepository;

    public GetStudentTemplateHandler(IClassRepository classRepository)
    {
        _classRepository = classRepository;
    }

    public async Task<ClassStudentTemplate> Handle(int classId, int currentUserId, CancellationToken cancellationToken = default)
    {
        var classEntity = await _classRepository.GetByIdAsync(classId, cancellationToken);
        if (classEntity == null)
            throw new KeyNotFoundException("Không tìm thấy lớp học.");

        return new ClassStudentTemplate
        {
            ClassId = classId,
            ClassCode = classEntity.ClassCode,
            CourseName = classEntity.Course?.Title ?? ""
        };
    }
}

public class ClassStudentTemplate
{
    public int ClassId { get; set; }
    public string ClassCode { get; set; } = "";
    public string CourseName { get; set; } = "";
}
