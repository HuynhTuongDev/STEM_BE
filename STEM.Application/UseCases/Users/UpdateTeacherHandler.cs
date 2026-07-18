using Microsoft.Extensions.Logging;
using STEM.Application.Dtos.Users;
using STEM.Core.Entities.Users;
using STEM.Core.Repository;
using FluentValidation;

namespace STEM.Application.UseCases.Users;

public class UpdateTeacherHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IValidator<UpdateTeacherRequest> _validator;
    private readonly ILogger<UpdateTeacherHandler> _logger;

    public UpdateTeacherHandler(
        IUserRepository userRepository,
        IValidator<UpdateTeacherRequest> validator,
        ILogger<UpdateTeacherHandler> logger)
    {
        _userRepository = userRepository;
        _validator = validator;
        _logger = logger;
    }

    public async Task<UserDetailResponse?> Handle(
        UpdateTeacherRequest request,
        int currentUserId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("UpdateTeacher called - TeacherId: {TeacherId}, IsActive: {IsActive}", request.Id, request.IsActive);

        await _validator.ValidateAndThrowAsync(request, cancellationToken);

        var currentUser = await _userRepository.GetByIdAsync(currentUserId, cancellationToken);
        if (currentUser == null)
            throw new UnauthorizedAccessException("Current user not found.");

        if (currentUser.Role?.Name != RoleNames.SchoolAdministrator)
            throw new UnauthorizedAccessException("Only School Administrator can update teachers.");

        var teacher = await _userRepository.GetByIdAsync(request.Id, cancellationToken);
        if (teacher == null)
            return null;

        if (teacher.Role?.Name != RoleNames.Teacher)
            throw new InvalidOperationException("The specified user is not a teacher.");

        if (teacher.SchoolId != currentUser.SchoolId)
            throw new UnauthorizedAccessException("You can only update teachers from your own school.");

        if (!string.IsNullOrEmpty(request.FullName))
            teacher.FullName = request.FullName;
        if (request.Phone != null)
            teacher.Phone = request.Phone;
        if (request.Avatar != null)
            teacher.Avatar = request.Avatar;
        if (request.Gender != null)
            teacher.Gender = request.Gender;
        if (request.DateOfBirth.HasValue)
            teacher.DateOfBirth = new DateOnly(request.DateOfBirth.Value.Year, request.DateOfBirth.Value.Month, request.DateOfBirth.Value.Day);
        if (request.Address != null)
            teacher.Address = request.Address;
        teacher.IsActive = request.IsActive;
        teacher.UpdatedAt = DateTime.UtcNow;

        _userRepository.Update(teacher);
        await _userRepository.SaveChangesAsync(cancellationToken);

        return new UserDetailResponse
        {
            UserId = teacher.Id,
            Email = teacher.Email,
            FullName = teacher.FullName,
            Phone = teacher.Phone,
            Avatar = teacher.Avatar,
            Gender = teacher.Gender,
            DateOfBirth = teacher.DateOfBirth,
            Address = teacher.Address,
            IsActive = teacher.IsActive,
            IsEmailVerified = teacher.IsEmailVerified,
            RoleId = teacher.RoleId,
            Role = teacher.Role?.Name ?? string.Empty,
            SchoolId = teacher.SchoolId,
            CreatedAt = teacher.CreatedAt,
            UpdatedAt = teacher.UpdatedAt
        };
    }
}
