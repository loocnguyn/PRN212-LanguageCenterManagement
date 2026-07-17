namespace BusinessObjects;

public class InvoicePricingInfo
{
    public decimal OriginalAmount { get; set; }
    public int? DiscountId { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal FinalAmount { get; set; }
    public DateOnly? DiscountDeadline { get; set; }
    public string DiscountStatus { get; set; } = "NONE";
}
