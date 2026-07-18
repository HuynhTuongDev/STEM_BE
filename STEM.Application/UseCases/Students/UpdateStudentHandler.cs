using Microsoft.Extensions.Logging;
using STEM.Application.Dtos.Students;
using STEM.Core.Entities.Users;
using STEM.Core.Repository;
using FluentValidation;

namespace STEM.Application.UseCases.Students;

public class UpdateStudentHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IValidator<UpdateStudentRequest> _validator;
    private readonly ILogger<UpdateStudentHandler> _logger;

    public UpdateStudentHandler(
        IUserRepository userRepository,
        IValidator<UpdateStudentRequest> validator,
        ILogger<UpdateStudentHandler> logger)
    {
        _userRepository = userRepository;
        _validator = validator;
        _logger = logger;
    }

    public async Task<StudentResponse?> Handle(
        int studentId,
        UpdateStudentRequest request,
        int currentUserId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("UpdateStudent called - StudentId: {StudentId}, Request.FullName: {FullName}, IsActive: {IsActive}",
            studentId, request.FullName, request.IsActive);

        await _validator.ValidateAndThrowAsync(request, cancellationToken);

        var currentUser = await _userRepository.GetByIdAsync(currentUserId, cancellationToken);
        if (currentUser == null)
            throw new UnauthorizedAccessException("Current user not found.");

        if (currentUser.Role?.Name != RoleNames.SchoolAdministrator)
            throw new UnauthorizedAccessException("Only School Administrator can update students.");

        var student = await _userRepository.GetByIdAsync(studentId, cancellationToken);
        if (student == null)
            return null;

        if (student.Role?.Name != RoleNames.Student)
            throw new InvalidOperationException("The specified user is not a student.");

        if (student.SchoolId != currentUser.SchoolId)
            throw new UnauthorizedAccessException("You can only update students from your own school.");

        if (!string.IsNullOrEmpty(request.FullName))
            student.FullName = request.FullName;
        if (request.Phone != null)
            student.Phone = request.Phone;
        if (request.Avatar != null)
            student.Avatar = request.Avatar;
        if (request.Gender != null)
            student.Gender = request.Gender;
        if (request.DateOfBirth.HasValue)
            student.DateOfBirth = new DateOnly(request.DateOfBirth.Value.Year, request.DateOfBirth.Value.Month, request.DateOfBirth.Value.Day);
        if (request.Address != null)
            student.Address = request.Address;
        student.IsActive = request.IsActive;
        student.UpdatedAt = DateTime.UtcNow;

        _userRepository.Update(student);
        await _userRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("UpdateStudent SUCCESS - StudentId: {StudentId}, FullName: {FullName}, IsActive: {IsActive}",
            student.Id, student.FullName, student.IsActive);

        return new StudentResponse
        {
            Id = student.Id,
            FullName = student.FullName,
            Email = student.Email,
            Phone = student.Phone,
            Avatar = student.Avatar,
            Gender = student.Gender,
            DateOfBirth = student.DateOfBirth,
            Address = student.Address,
            IsActive = student.IsActive,
            IsEmailVerified = student.IsEmailVerified,
            SchoolId = student.SchoolId,
            SchoolName = student.School?.Name,
            TotalEnrolledClasses = 0,
            CertificatesEarned = 0,
            AverageScore = null,
            CreatedAt = student.CreatedAt,
            UpdatedAt = student.UpdatedAt
        };
    }
}
