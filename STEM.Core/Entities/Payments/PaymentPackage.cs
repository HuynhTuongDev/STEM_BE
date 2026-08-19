namespace STEM.Core.Entities.Payments;

public enum PaymentStatus
{
    Pending,
    Processing,
    Completed,
    Failed,
    Cancelled,
    Refunded,
    Expired
}

public enum PaymentMethod
{
    PayOS,
    BankTransfer,
    Other
}

public class PaymentPackage : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string Currency { get; set; } = "VND";
    public int TokenAmount { get; set; }
    public int StudentLimit { get; set; }  // Số học sinh tối đa
    public int DurationMonths { get; set; }  // Thời hạn gói (tháng)
    public bool IsActive { get; set; } = true;
    public bool IsFeatured { get; set; }
    public string? Features { get; set; }
    public int DisplayOrder { get; set; }
    public DateTime ExpiresAt { get; set; }

    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
