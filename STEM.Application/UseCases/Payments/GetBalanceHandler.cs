using Microsoft.Extensions.Logging;
using STEM.Application.DTOs.Payments;
using STEM.Core.Interfaces;
using STEM.Core.Repository;

namespace STEM.Application.UseCases.Payments;

public class GetBalanceHandler
{
    private readonly ITokenAccountRepository _accountRepository;
    private readonly ITokenAllocationRepository _allocationRepository;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<GetBalanceHandler> _logger;

    public GetBalanceHandler(
        ITokenAccountRepository accountRepository,
        ITokenAllocationRepository allocationRepository,
        IUserRepository userRepository,
        ILogger<GetBalanceHandler> logger)
    {
        _accountRepository = accountRepository;
        _allocationRepository = allocationRepository;
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<TokenBalanceDto?> Handle(int schoolId, string schoolName, CancellationToken cancellationToken = default)
    {
        try
        {
            var account = await _accountRepository.GetBySchoolIdAsync(schoolId, cancellationToken);
            
            if (account == null)
            {
                var (tc, sc) = await _userRepository.GetTeacherStudentCountBySchoolAsync(schoolId, cancellationToken);
                return new TokenBalanceDto(
                    SchoolId: schoolId,
                    SchoolName: schoolName,
                    TotalTokensPurchased: 0,
                    TokensRemaining: 0,
                    TokensDistributed: 0,
                    TokensUsed: 0,
                    ExpiresAt: null,
                    LastPurchaseAt: null,
                    TeacherCount: tc,
                    StudentCount: sc,
                    TeacherTokens: 0,
                    StudentTokens: 0
                );
            }

            var allocatedTokens = await _allocationRepository.GetTotalAllocatedByAccountIdAsync(account.Id, cancellationToken);
            var (allocTeacherCount, allocStudentCount, teacherTokens, studentTokens) = 
                await _allocationRepository.GetAllocationStatsByRoleAsync(account.Id, cancellationToken);
            
            int teacherCount = allocTeacherCount;
            int studentCount = allocStudentCount;
            
            // Fallback: get user counts from User table if allocation stats are 0
            if (teacherCount == 0 && studentCount == 0)
            {
                var (userTc, userSc) = await _userRepository.GetTeacherStudentCountBySchoolAsync(schoolId, cancellationToken);
                teacherCount = userTc;
                studentCount = userSc;
            }

            return new TokenBalanceDto(
                SchoolId: schoolId,
                SchoolName: schoolName,
                TotalTokensPurchased: account.TotalTokensPurchased,
                TokensRemaining: account.TokensRemaining,
                TokensDistributed: allocatedTokens,
                TokensUsed: account.TokensUsed,
                ExpiresAt: account.ExpiresAt,
                LastPurchaseAt: account.LastPurchaseAt,
                TeacherCount: teacherCount,
                StudentCount: studentCount,
                TeacherTokens: teacherTokens,
                StudentTokens: studentTokens
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting balance for school {SchoolId}", schoolId);
            return null;
        }
    }
}
