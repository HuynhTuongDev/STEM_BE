using STEM.Application.Dtos.Users;
using STEM.Core.Entities.Users;
using STEM.Core.Repository;
using FluentValidation;

namespace STEM.Application.UseCases.Users;

public class UpdateTeacherHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IValidator<UpdateTeacherRequest> _validator;

    public UpdateTeacherHandler(
        IUserRepository userRepository,
        IValidator<UpdateTeacherRequest> validator)
    {
        _userRepository = userRepository;
        _validator = validator;
    }

    public async Task<bool> Handle(
        UpdateTeacherRequest request,
        int currentUserId,
        CancellationToken cancellationToken = default)
    {
        await _validator.ValidateAndThrowAsync(request, cancellationToken);

        var currentUser = await _userRepository.GetByIdAsync(currentUserId, cancellationToken);
        if (currentUser == null)
            throw new UnauthorizedAccessException("Current user not found.");

        if (currentUser.Role?.Name != RoleNames.SchoolAdministrator)
            throw new UnauthorizedAccessException("Only School Administrator can update teachers.");

        var teacher = await _userRepository.GetByIdAsync(request.Id, cancellationToken);
        if (teacher == null)
            return false;

        if (teacher.Role?.Name != RoleNames.Teacher)
            throw new InvalidOperationException("The specified user is not a teacher.");

        if (teacher.SchoolId != currentUser.SchoolId)
            throw new UnauthorizedAccessException("You can only update teachers from your own school.");

        teacher.FullName = request.FullName;
        teacher.Phone = request.Phone;
        teacher.Avatar = request.Avatar;
        teacher.Gender = request.Gender;
        teacher.DateOfBirth = request.DateOfBirth.HasValue
            ? new DateOnly(request.DateOfBirth.Value.Year, request.DateOfBirth.Value.Month, request.DateOfBirth.Value.Day)
            : null;
        teacher.Address = request.Address;
        teacher.IsActive = request.IsActive;
        teacher.UpdatedAt = DateTime.UtcNow;

        _userRepository.Update(teacher);
        await _userRepository.SaveChangesAsync(cancellationToken);

        return true;
    }
}
