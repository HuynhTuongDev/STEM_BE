using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using STEM.Core.Entities.Schools;

namespace STEM.Core.Entities.Payments;

public class Payment : BaseEntity
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public override int Id { get; set; }
    public string TransactionId { get; set; } = string.Empty;
    public long? OrderCode { get; set; }
    public int PackageId { get; set; }
    public int? SchoolId { get; set; }
    public int? UserId { get; set; }
    public int TokenAmount { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "VND";
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public PaymentMethod Method { get; set; } = PaymentMethod.PayOS;
    public string? GatewayTransactionId { get; set; }
    public string? PaymentLinkId { get; set; }
    public string? CheckoutUrl { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime? CanceledAt { get; set; }
    public string? CancellationReason { get; set; }
    public string? Metadata { get; set; }

    public PaymentPackage? Package { get; set; }
    public School? School { get; set; }
    public ICollection<TokenTransaction> Transactions { get; set; } = new List<TokenTransaction>();
}
