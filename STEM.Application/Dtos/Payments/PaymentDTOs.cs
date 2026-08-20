namespace STEM.Application.DTOs.Payments;

public record GetPackagesResponse(List<PackageDto> Packages);
public record PackageDto(
    int Id,
    string Name,
    string? Description,
    decimal Price,
    string Currency,
    int TokenAmount,
    int StudentLimit,
    bool IsActive,
    bool IsFeatured,
    string? Features,
    DateTime ExpiresAt
);

public record CreatePaymentRequest(
    [property: System.Text.Json.Serialization.JsonPropertyName("packageId")]
    int PackageId, 
    [property: System.Text.Json.Serialization.JsonPropertyName("method")]
    string Method = "PayOS"
);
public record CreatePaymentResponse(
    bool Success,
    int PaymentId,
    string TransactionId,
    string? CheckoutUrl,
    string? PaymentLinkId,
    decimal Amount,
    string Currency,
    string Status,
    string? ErrorMessage
);

public record PaymentDto(
    int Id,
    string TransactionId,
    int PackageId,
    string PackageName,
    int DurationMonths,
    int TokenAmount,
    decimal Amount,
    string Currency,
    string Status,
    string Method,
    string? GatewayTransactionId,
    DateTime? PaidAt,
    DateTime? ExpiresAt,
    DateTime CreatedAt
);

public record PaymentListResponse(
    List<PaymentDto> Items,
    int Total,
    int Page,
    int PageSize
);

public record TokenBalanceDto(
    int SchoolId,
    string SchoolName,
    int TotalTokensPurchased,
    int TokensRemaining,
    int TokensDistributed,
    int TokensUsed,
    DateTime? ExpiresAt,
    DateTime? LastPurchaseAt
);

public record TokenTransactionDto(
    int Id,
    int PaymentId,
    string Type,
    int Quantity,
    int BalanceAfter,
    string? Description,
    DateTime CreatedAt
);

public record TokenAllocationDto(
    int Id,
    int SchoolId,
    int UserId,
    string UserName,
    string UserEmail,
    string UserRole,
    int AllocatedTokens,
    int UsedTokens,
    int RemainingTokens,
    DateTime? ExpiresAt,
    string? Notes,
    int AllocatedByUserId,
    string AllocatedByUserName,
    DateTime CreatedAt
);

public record TokenAllocationListResponse(
    List<TokenAllocationDto> Items,
    int Total,
    int Page,
    int PageSize
);

public record AllocateTokensRequest(int UserId, int Tokens, DateTime? ExpiresAt = null, string? Notes = null);
public record AllocateTokensResponse(
    bool Success,
    int AllocationId,
    int UserId,
    string UserName,
    int TokensAllocated,
    int SchoolTokensRemaining,
    DateTime? ExpiresAt,
    string? ErrorMessage
);

public record UserTokenInfoDto(
    int UserId,
    string UserName,
    string Email,
    string Role,
    int AllocatedTokens,
    int TokensAllocated,
    int SchoolTokensRemaining,
    DateTime? ExpiresAt,
    string? ErrorMessage
);

// Bulk Allocation DTOs
public record BulkAllocationByRoleRequest(
    int StudentTokens,
    int TeacherTokens,
    DateTime? ExpiresAt = null,
    string? Notes = null
);

public record BulkAllocationResult(
    int UserId,
    string UserName,
    string Role,
    bool Success,
    string? ErrorMessage,
    int TokensAllocated
);

public record BulkAllocationResponse(
    bool Success,
    int TotalUsers,
    int SuccessCount,
    int FailedCount,
    int TotalTokensAllocated,
    int SchoolTokensRemaining,
    List<BulkAllocationResult> Results,
    string? ErrorMessage
);