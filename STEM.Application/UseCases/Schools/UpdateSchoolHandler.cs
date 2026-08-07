using FluentValidation;
using STEM.Application.Dtos.Schools;
using STEM.Core.Entities.Schools;
using STEM.Core.Repository;

namespace STEM.Application.UseCases.Schools;

public class UpdateSchoolHandler
{
    private readonly IRepository<School> _schoolRepository;
    private readonly IUserRepository _userRepository;

    public UpdateSchoolHandler(
        IRepository<School> schoolRepository,
        IUserRepository userRepository)
    {
        _schoolRepository = schoolRepository;
        _userRepository = userRepository;
    }

    public async Task Handle(int id, UpdateSchoolRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("School name is required.");

        if (string.IsNullOrWhiteSpace(request.Address))
            throw new ArgumentException("School address is required.");

        var school = await _schoolRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"School with id {id} not found.");

        var oldEmail = school.RepresentativeEmail;

        school.Name = request.Name.Trim();
        school.Address = request.Address.Trim();
        school.RepresentativeName = request.RepresentativeName.Trim();
        school.RepresentativeEmail = request.RepresentativeEmail.Trim();
        school.ProofOfActivity = request.ProofOfActivity;
        school.StudentScale = request.StudentScale;
        school.RepresentativePosition = request.RepresentativePosition;
        school.Website = request.Website;
        school.Notes = request.Notes;
        school.UpdatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);

        _schoolRepository.Update(school);
        await _schoolRepository.SaveChangesAsync(cancellationToken);

        // Update representative user details if they exist
        var adminUser = await _userRepository.GetByEmailAsync(oldEmail, cancellationToken);
        if (adminUser != null)
        {
            adminUser.Email = school.RepresentativeEmail;
            adminUser.FullName = school.RepresentativeName;
            if (request.Phone != null)
            {
                adminUser.Phone = request.Phone;
            }
            adminUser.UpdatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);
            _userRepository.Update(adminUser);
            await _userRepository.SaveChangesAsync(cancellationToken);
        }
    }
}
