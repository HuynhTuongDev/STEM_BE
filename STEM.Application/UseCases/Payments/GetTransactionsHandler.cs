using Microsoft.Extensions.Logging;
using STEM.Application.DTOs.Payments;
using STEM.Core.Interfaces;

namespace STEM.Application.UseCases.Payments;

public class GetTransactionsHandler
{
    private readonly ITokenAccountRepository _accountRepository;
    private readonly ITokenTransactionRepository _transactionRepository;
    private readonly ILogger<GetTransactionsHandler> _logger;

    public GetTransactionsHandler(
        ITokenAccountRepository accountRepository,
        ITokenTransactionRepository transactionRepository,
        ILogger<GetTransactionsHandler> logger)
    {
        _accountRepository = accountRepository;
        _transactionRepository = transactionRepository;
        _logger = logger;
    }

    public async Task<List<TokenTransactionDto>> Handle(
        int schoolId,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var account = await _accountRepository.GetBySchoolIdAsync(schoolId, cancellationToken);
            if (account == null)
            {
                return new List<TokenTransactionDto>();
            }

            var transactions = await _transactionRepository.GetByAccountIdAsync(account.Id, page, pageSize, cancellationToken);

            return transactions.Select(t => new TokenTransactionDto(
                t.Id,
                t.PaymentId ?? 0,
                t.Type.ToString().ToUpper(),
                t.Quantity,
                t.BalanceAfter,
                t.Description,
                t.CreatedAt
            )).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting transactions for school {SchoolId}", schoolId);
            return new List<TokenTransactionDto>();
        }
    }
}
