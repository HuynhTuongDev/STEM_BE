using STEM.Core.Entities.Schools;
using STEM.Core.Repository;

namespace STEM.Application.UseCases.Schools;

public class DeleteSchoolHandler
{
    private readonly IRepository<School> _schoolRepository;

    public DeleteSchoolHandler(IRepository<School> schoolRepository)
    {
        _schoolRepository = schoolRepository;
    }

    public async Task Handle(int id, CancellationToken cancellationToken = default)
    {
        var school = await _schoolRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"School with id {id} not found.");

        _schoolRepository.Delete(school);
        await _schoolRepository.SaveChangesAsync(cancellationToken);
    }
}
