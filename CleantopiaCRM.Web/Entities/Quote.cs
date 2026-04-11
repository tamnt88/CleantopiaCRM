using System.ComponentModel.DataAnnotations;

namespace CleantopiaCRM.Web.Entities;

public class Quote
{
    public int Id { get; set; }
    [MaxLength(50)]
    public string QuoteNo { get; set; } = string.Empty;
    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public DateTime QuoteDate { get; set; } = DateTime.Today;
    public DateTime? ValidUntil { get; set; }
    [MaxLength(50)]
    public string Status { get; set; } = "Draft";
    public int? ServiceAddressId { get; set; }
    public int? ServiceProvinceId { get; set; }
    public int? ServiceWardId { get; set; }
    [MaxLength(1000)]
    public string? ServiceAddressText { get; set; }
    [MaxLength(250)]
    public string? ContactName { get; set; }
    [MaxLength(20)]
    public string? ContactPhone { get; set; }
    public bool HasVatInvoice { get; set; }
    [MaxLength(250)]
    public string? InvoiceCompanyName { get; set; }
    [MaxLength(50)]
    public string? InvoiceTaxCode { get; set; }
    [MaxLength(500)]
    public string? InvoiceAddress { get; set; }
    [MaxLength(250)]
    public string? InvoiceEmail { get; set; }
    [MaxLength(250)]
    public string? InvoiceReceiver { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal VatRate { get; set; } = 8;
    public decimal SubtotalAmount { get; set; }
    public decimal VatAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string? Notes { get; set; }
    public ICollection<QuoteItem> Items { get; set; } = new List<QuoteItem>();
}
