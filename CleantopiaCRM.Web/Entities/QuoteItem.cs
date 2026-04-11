namespace CleantopiaCRM.Web.Entities;

public class QuoteItem
{
    public int Id { get; set; }
    public int QuoteId { get; set; }
    public Quote? Quote { get; set; }
    public int ServicePriceId { get; set; }
    public ServicePrice? ServicePrice { get; set; }
    public decimal Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public string? Note { get; set; }
    public decimal Amount => (Quantity * UnitPrice) - DiscountAmount < 0 ? 0 : (Quantity * UnitPrice) - DiscountAmount;
}
