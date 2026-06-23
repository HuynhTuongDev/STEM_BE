using FluentValidation;
using STEM.Application.Dtos.Schools;
using STEM.Core.Entities.Schools;
using STEM.Core.Repository;

namespace STEM.Application.UseCases.Schools;

public class UpdateSchoolHandler
{
    private readonly IRepository<School> _schoolRepository;

    public UpdateSchoolHandler(IRepository<School> schoolRepository)
    {
        _schoolRepository = schoolRepository;
    }

    public async Task Handle(int id, UpdateSchoolRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("School name is required.");

        if (string.IsNullOrWhiteSpace(request.Address))
            throw new ArgumentException("School address is required.");

        var school = await _schoolRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"School with id {id} not found.");

        school.Name = request.Name.Trim();
        school.Address = request.Address.Trim();
        school.RepresentativeName = request.RepresentativeName.Trim();
        school.RepresentativeEmail = request.RepresentativeEmail.Trim();
        school.ProofOfActivity = request.ProofOfActivity;
        school.UpdatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);

        _schoolRepository.Update(school);
        await _schoolRepository.SaveChangesAsync(cancellationToken);
    }
}
