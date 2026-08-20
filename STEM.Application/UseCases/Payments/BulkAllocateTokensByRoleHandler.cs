using Microsoft.Extensions.Logging;
using STEM.Application.DTOs.Payments;
using STEM.Core.Entities.Payments;
using STEM.Core.Entities.Users;
using STEM.Core.Interfaces;
using STEM.Core.Repository;

namespace STEM.Application.UseCases.Payments;

public class BulkAllocateTokensByRoleHandler
{
    private readonly ITokenAccountRepository _accountRepository;
    private readonly ITokenAllocationRepository _allocationRepository;
    private readonly ITokenTransactionRepository _transactionRepository;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<BulkAllocateTokensByRoleHandler> _logger;

    public BulkAllocateTokensByRoleHandler(
        ITokenAccountRepository accountRepository,
        ITokenAllocationRepository allocationRepository,
        ITokenTransactionRepository transactionRepository,
        IUserRepository userRepository,
        ILogger<BulkAllocateTokensByRoleHandler> logger)
    {
        _accountRepository = accountRepository;
        _allocationRepository = allocationRepository;
        _transactionRepository = transactionRepository;
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<BulkAllocationResponse> Handle(
        BulkAllocationByRoleRequest request,
        int schoolId,
        int allocatedByUserId,
        CancellationToken cancellationToken = default)
    {
        var results = new List<BulkAllocationResult>();
        int totalTokensToAllocate = 0;
        int successCount = 0;
        int failedCount = 0;

        try
        {
            var account = await _accountRepository.GetBySchoolIdAsync(schoolId, cancellationToken);
            if (account == null)
            {
                return new BulkAllocationResponse(
                    Success: false,
                    TotalUsers: 0,
                    SuccessCount: 0,
                    FailedCount: 0,
                    TotalTokensAllocated: 0,
                    SchoolTokensRemaining: 0,
                    Results: new List<BulkAllocationResult>(),
                    ErrorMessage: "School account not found"
                );
            }

            // Get all users in school by role
            var allUsers = await _userRepository.GetBySchoolIdAsync(schoolId, cancellationToken);
            var usersByRole = allUsers.ToList();

            // Calculate total tokens needed
            int studentCount = usersByRole.Count(u => u.Role?.Name == RoleNames.Student);
            int teacherCount = usersByRole.Count(u => u.Role?.Name == RoleNames.Teacher);

            totalTokensToAllocate = (studentCount * request.StudentTokens) + (teacherCount * request.TeacherTokens);

            // Check if school has enough tokens
            if (account.TokensRemaining < totalTokensToAllocate)
            {
                return new BulkAllocationResponse(
                    Success: false,
                    TotalUsers: studentCount + teacherCount,
                    SuccessCount: 0,
                    FailedCount: 0,
                    TotalTokensAllocated: 0,
                    SchoolTokensRemaining: account.TokensRemaining,
                    Results: new List<BulkAllocationResult>(),
                    ErrorMessage: $"Insufficient tokens. Available: {account.TokensRemaining}, Required: {totalTokensToAllocate} (Students: {studentCount} x {request.StudentTokens} + Teachers: {teacherCount} x {request.TeacherTokens})"
                );
            }

            // Process students
            var students = usersByRole.Where(u => u.Role?.Name == RoleNames.Student).ToList();
            foreach (var student in students)
            {
                var result = await AllocateToUser(
                    student, 
                    request.StudentTokens, 
                    request.ExpiresAt, 
                    request.Notes, 
                    account,
                    allocatedByUserId, 
                    cancellationToken);
                
                results.Add(result);
                if (result.Success) successCount++;
                else failedCount++;
            }

            // Process teachers
            var teachers = usersByRole.Where(u => u.Role?.Name == RoleNames.Teacher).ToList();
            foreach (var teacher in teachers)
            {
                var result = await AllocateToUser(
                    teacher, 
                    request.TeacherTokens, 
                    request.ExpiresAt, 
                    request.Notes, 
                    account,
                    allocatedByUserId, 
                    cancellationToken);
                
                results.Add(result);
                if (result.Success) successCount++;
                else failedCount++;
            }

            // Save all changes
            await _accountRepository.SaveChangesAsync(cancellationToken);
            await _allocationRepository.SaveChangesAsync(cancellationToken);
            await _transactionRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Bulk allocation completed: {SuccessCount}/{Total} users, {TotalTokens} tokens allocated by {AllocatedBy}",
                successCount, studentCount + teacherCount, totalTokensToAllocate, allocatedByUserId);

            return new BulkAllocationResponse(
                Success: true,
                TotalUsers: studentCount + teacherCount,
                SuccessCount: successCount,
                FailedCount: failedCount,
                TotalTokensAllocated: totalTokensToAllocate,
                SchoolTokensRemaining: account.TokensRemaining,
                Results: results,
                ErrorMessage: null
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in bulk token allocation");
            return new BulkAllocationResponse(
                Success: false,
                TotalUsers: 0,
                SuccessCount: successCount,
                FailedCount: failedCount,
                TotalTokensAllocated: 0,
                SchoolTokensRemaining: 0,
                Results: results,
                ErrorMessage: ex.Message
            );
        }
    }

    private async Task<BulkAllocationResult> AllocateToUser(
        User user,
        int tokens,
        DateTime? expiresAt,
        string? notes,
        TokenAccount account,
        int allocatedByUserId,
        CancellationToken cancellationToken)
    {
        try
        {
            var existingAllocation = await _allocationRepository.GetActiveAllocationAsync(account.Id, user.Id, cancellationToken);
            int totalTokensForUser;
            DateTime? expiresAtUtc = expiresAt.HasValue
                ? DateTime.SpecifyKind(expiresAt.Value, DateTimeKind.Utc)
                : (account.ExpiresAt.HasValue ? DateTime.SpecifyKind(account.ExpiresAt.Value, DateTimeKind.Utc) : null);

            if (existingAllocation != null)
            {
                totalTokensForUser = existingAllocation.AllocatedTokens + tokens;
                existingAllocation.AllocatedTokens = totalTokensForUser;
                existingAllocation.Notes = notes;
                existingAllocation.ExpiresAt = expiresAtUtc;
                existingAllocation.UpdatedAt = DateTime.UtcNow;
                _allocationRepository.Update(existingAllocation);
            }
            else
            {
                var newAllocation = new TokenAllocation
                {
                    AccountId = account.Id,
                    UserId = user.Id,
                    AllocatedTokens = tokens,
                    UsedTokens = 0,
                    ExpiresAt = expiresAtUtc,
                    Notes = notes,
                    AllocatedByUserId = allocatedByUserId,
                    IsActive = true
                };
                await _allocationRepository.AddAsync(newAllocation, cancellationToken);
                existingAllocation = newAllocation;
                totalTokensForUser = tokens;
            }

            // Deduct tokens from school account
            account.TokensRemaining -= tokens;

            // Create transaction record
            var transaction = new TokenTransaction
            {
                AccountId = account.Id,
                Type = TransactionType.Distribution,
                Quantity = -tokens,
                BalanceAfter = account.TokensRemaining,
                Description = $"Bulk allocation: {tokens} tokens to {user.FullName} ({user.Role?.Name}). Total: {totalTokensForUser}",
                ReferenceId = existingAllocation.Id.ToString()
            };
            await _transactionRepository.AddAsync(transaction, cancellationToken);

            return new BulkAllocationResult(
                UserId: user.Id,
                UserName: user.FullName,
                Role: user.Role?.Name ?? "Unknown",
                Success: true,
                ErrorMessage: null,
                TokensAllocated: totalTokensForUser
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to allocate tokens to user {UserId}", user.Id);
            return new BulkAllocationResult(
                UserId: user.Id,
                UserName: user.FullName,
                Role: user.Role?.Name ?? "Unknown",
                Success: false,
                ErrorMessage: ex.Message,
                TokensAllocated: 0
            );
        }
    }
}
