using STEM.Application.Dtos.Payments;
using STEM.Core.Entities.Payments;
using STEM.Core.Entities.Users;
using STEM.Core.Repository;

namespace STEM.Application.UseCases.Payments;

public class GetPackagesHandler
{
    private readonly IPaymentPackageRepository _packageRepository;

    public GetPackagesHandler(IPaymentPackageRepository packageRepository)
    {
        _packageRepository = packageRepository;
    }

    public async Task<IEnumerable<PaymentPackageResponse>> Handle(CancellationToken cancellationToken = default)
    {
        var packages = await _packageRepository.GetActivePackagesAsync(cancellationToken);
        return packages.Select(MapToResponse);
    }

    private static PaymentPackageResponse MapToResponse(PaymentPackage pkg) => new()
    {
        Id = pkg.Id,
        Name = pkg.Name,
        Description = pkg.Description,
        DurationMonths = pkg.DurationMonths,
        Price = pkg.Price,
        Currency = pkg.Currency,
        TokenAmount = pkg.TokenAmount,
        IsActive = pkg.IsActive,
        IsFeatured = pkg.IsFeatured,
        Features = pkg.Features
    };
}

public class CreatePaymentHandler
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IPaymentPackageRepository _packageRepository;
    private readonly IUserRepository _userRepository;
    private readonly ITokenAccountRepository _tokenAccountRepository;

    public CreatePaymentHandler(
        IPaymentRepository paymentRepository,
        IPaymentPackageRepository packageRepository,
        IUserRepository userRepository,
        ITokenAccountRepository tokenAccountRepository)
    {
        _paymentRepository = paymentRepository;
        _packageRepository = packageRepository;
        _userRepository = userRepository;
        _tokenAccountRepository = tokenAccountRepository;
    }

    public async Task<PaymentResponse> Handle(CreatePaymentRequest request, int buyerId, CancellationToken cancellationToken = default)
    {
        // Get package
        var package = await _packageRepository.GetByIdWithDetailsAsync(request.PackageId, cancellationToken);
        if (package == null || !package.IsActive)
            throw new KeyNotFoundException("Package not found or inactive");

        // Get buyer (School Admin)
        var buyer = await _userRepository.GetByIdAsync(buyerId, cancellationToken);
        if (buyer == null)
            throw new UnauthorizedAccessException("User not found");

        // Get Master Admin (seller) - first Master Admin user
        var masterAdmin = (await _userRepository.GetUsersPagedAsync(1, 1, null, 1, null, null, cancellationToken)).Users.FirstOrDefault();
        if (masterAdmin == null)
            throw new InvalidOperationException("No Master Admin found");

        // Create payment record
        var payment = new Payment
        {
            TransactionId = Guid.NewGuid().ToString(),
            BuyerId = buyerId,
            SellerId = masterAdmin.Id,
            PackageId = package.Id,
            Amount = package.Price,
            Currency = package.Currency,
            Status = PaymentStatus.Pending,
            Method = request.Method,
            TokenQuantity = package.TokenAmount,
            TokensRemaining = package.TokenAmount
        };

        await _paymentRepository.AddAsync(payment, cancellationToken);

        return MapToResponse(payment, package);
    }

    private static PaymentResponse MapToResponse(Payment payment, PaymentPackage package) => new()
    {
        Id = payment.Id,
        TransactionId = payment.TransactionId,
        PackageId = package.Id,
        PackageName = package.Name,
        DurationMonths = package.DurationMonths,
        TokenAmount = package.TokenAmount,
        Amount = payment.Amount,
        Currency = payment.Currency,
        Status = payment.Status.ToString(),
        Method = payment.Method.ToString(),
        PaidAt = payment.PaidAt,
        ExpiresAt = payment.ExpiresAt,
        PaymentGateway = payment.PaymentGateway,
        GatewayTransactionId = payment.GatewayTransactionId,
        CreatedAt = payment.CreatedAt
    };
}

public class PaymentCallbackHandler
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IPaymentPackageRepository _packageRepository;
    private readonly ITokenAccountRepository _tokenAccountRepository;
    private readonly ITokenTransactionRepository _tokenTransactionRepository;
    private readonly IUserRepository _userRepository;

    public PaymentCallbackHandler(
        IPaymentRepository paymentRepository,
        IPaymentPackageRepository packageRepository,
        ITokenAccountRepository tokenAccountRepository,
        ITokenTransactionRepository tokenTransactionRepository,
        IUserRepository userRepository)
    {
        _paymentRepository = paymentRepository;
        _packageRepository = packageRepository;
        _tokenAccountRepository = tokenAccountRepository;
        _tokenTransactionRepository = tokenTransactionRepository;
        _userRepository = userRepository;
    }

    public async Task<bool> Handle(PaymentCallbackRequest request, CancellationToken cancellationToken = default)
    {
        var payment = await _paymentRepository.GetByTransactionIdAsync(request.TransactionId, cancellationToken);
        if (payment == null)
            throw new KeyNotFoundException("Payment not found");

        var package = await _packageRepository.GetByIdWithDetailsAsync(payment.PackageId, cancellationToken);

        if (request.Status.ToLower() == "success" || request.Status.ToLower() == "completed")
        {
            payment.Status = PaymentStatus.Completed;
            payment.PaidAt = DateTime.UtcNow;
            payment.GatewayTransactionId = request.GatewayTransactionId;
            payment.ExpiresAt = DateTime.UtcNow.AddMonths(package?.DurationMonths ?? 1);

            // Get buyer's school and add tokens
            var buyer = await _userRepository.GetByIdAsync(payment.BuyerId, cancellationToken);
            if (buyer?.SchoolId.HasValue == true)
            {
                var tokenAccount = await _tokenAccountRepository.GetOrCreateAsync(buyer.SchoolId.Value, cancellationToken);
                tokenAccount.TotalTokensPurchased += payment.TokenQuantity;
                tokenAccount.TokensRemaining += payment.TokenQuantity;
                tokenAccount.LastPurchaseAt = DateTime.UtcNow;

                if (tokenAccount.ExpiresAt == null || tokenAccount.ExpiresAt < payment.ExpiresAt)
                {
                    tokenAccount.ExpiresAt = payment.ExpiresAt;
                }

                _tokenAccountRepository.Update(tokenAccount);
                await _tokenAccountRepository.SaveChangesAsync(cancellationToken);

                // Create token transaction
                var transaction = new TokenTransaction
                {
                    SchoolId = buyer.SchoolId.Value,
                    PaymentId = payment.Id,
                    Type = TokenTransactionType.Purchase,
                    Quantity = payment.TokenQuantity,
                    BalanceAfter = tokenAccount.TokensRemaining,
                    Description = $"Purchase: {package?.Name}"
                };
                await _tokenTransactionRepository.AddAsync(transaction, cancellationToken);
            }
        }
        else
        {
            payment.Status = PaymentStatus.Failed;
            payment.FailureReason = request.Status;
        }

        _paymentRepository.Update(payment);
        await _paymentRepository.SaveChangesAsync(cancellationToken);
        return true;
    }
}

public class GetPaymentsHandler
{
    private readonly IPaymentRepository _paymentRepository;

    public GetPaymentsHandler(IPaymentRepository paymentRepository)
    {
        _paymentRepository = paymentRepository;
    }

    public async Task<PaymentListResponse> Handle(int schoolId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var (payments, total) = await _paymentRepository.GetBySchoolAsync(schoolId, page, pageSize, cancellationToken);
        var paymentList = payments.ToList();

        return new PaymentListResponse
        {
            Items = paymentList.Select(p => new PaymentResponse
            {
                Id = p.Id,
                TransactionId = p.TransactionId,
                PackageId = p.PackageId,
                PackageName = p.Package?.Name ?? "",
                DurationMonths = p.Package?.DurationMonths ?? 0,
                TokenAmount = p.TokenQuantity,
                Amount = p.Amount,
                Currency = p.Currency,
                Status = p.Status.ToString(),
                Method = p.Method.ToString(),
                PaidAt = p.PaidAt,
                ExpiresAt = p.ExpiresAt,
                PaymentGateway = p.PaymentGateway,
                GatewayTransactionId = p.GatewayTransactionId,
                CreatedAt = p.CreatedAt
            }).ToList(),
            Total = total,
            Page = page,
            PageSize = pageSize
        };
    }
}

public class GetTokenBalanceHandler
{
    private readonly ITokenAccountRepository _tokenAccountRepository;

    public GetTokenBalanceHandler(ITokenAccountRepository tokenAccountRepository)
    {
        _tokenAccountRepository = tokenAccountRepository;
    }

    public async Task<TokenBalanceResponse> Handle(int schoolId, string schoolName, CancellationToken cancellationToken = default)
    {
        var account = await _tokenAccountRepository.GetBySchoolIdAsync(schoolId, cancellationToken);
        
        return new TokenBalanceResponse
        {
            SchoolId = schoolId,
            SchoolName = schoolName,
            TotalTokensPurchased = account?.TotalTokensPurchased ?? 0,
            TokensRemaining = account?.TokensRemaining ?? 0,
            TokensUsed = account?.TokensUsed ?? 0,
            ExpiresAt = account?.ExpiresAt,
            LastPurchaseAt = account?.LastPurchaseAt
        };
    }
}

public class GetTokenTransactionsHandler
{
    private readonly ITokenTransactionRepository _transactionRepository;

    public GetTokenTransactionsHandler(ITokenTransactionRepository transactionRepository)
    {
        _transactionRepository = transactionRepository;
    }

    public async Task<IEnumerable<TokenTransactionResponse>> Handle(int schoolId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var transactions = await _transactionRepository.GetBySchoolAsync(schoolId, page, pageSize, cancellationToken);
        return transactions.Select(t => new TokenTransactionResponse
        {
            Id = t.Id,
            SchoolId = t.SchoolId,
            PaymentId = t.PaymentId,
            Type = t.Type.ToString(),
            Quantity = t.Quantity,
            BalanceAfter = t.BalanceAfter,
            Description = t.Description,
            CreatedAt = t.CreatedAt
        });
    }
}

public class UseTokenHandler
{
    private readonly ITokenAccountRepository _tokenAccountRepository;
    private readonly ITokenTransactionRepository _transactionRepository;

    public UseTokenHandler(
        ITokenAccountRepository tokenAccountRepository,
        ITokenTransactionRepository transactionRepository)
    {
        _tokenAccountRepository = tokenAccountRepository;
        _transactionRepository = transactionRepository;
    }

    public async Task<bool> Handle(int schoolId, int amount, string description, CancellationToken cancellationToken = default)
    {
        var account = await _tokenAccountRepository.GetBySchoolIdAsync(schoolId, cancellationToken);
        if (account == null || account.TokensRemaining < amount)
            throw new InvalidOperationException("Insufficient tokens");

        account.TokensRemaining -= amount;
        account.TokensUsed += amount;
        _tokenAccountRepository.Update(account);
        await _tokenAccountRepository.SaveChangesAsync(cancellationToken);

        var transaction = new TokenTransaction
        {
            SchoolId = schoolId,
            PaymentId = 0,
            Type = TokenTransactionType.Usage,
            Quantity = -amount,
            BalanceAfter = account.TokensRemaining,
            Description = description
        };
        await _transactionRepository.AddAsync(transaction, cancellationToken);

        return true;
    }
}
