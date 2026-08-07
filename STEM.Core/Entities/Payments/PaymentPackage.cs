namespace STEM.Core.Entities.Payments;

public class PaymentPackage : BaseEntity
{
    public string Name { get; set; } = string.Empty; // e.g., "1 Month", "3 Months"
    public string Description { get; set; } = string.Empty;
    public int DurationMonths { get; set; } // 1, 3, 6, 9, 12
    public decimal Price { get; set; }
    public string Currency { get; set; } = "USD";
    public int TokenAmount { get; set; } // Number of tokens included
    public bool IsActive { get; set; } = true;
    public bool IsFeatured { get; set; }
    public int SortOrder { get; set; }
    public string? Features { get; set; } // JSON array of features

    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
