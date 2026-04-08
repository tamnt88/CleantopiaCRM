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
    public string? Notes { get; set; }
    public ICollection<QuoteItem> Items { get; set; } = new List<QuoteItem>();
}
