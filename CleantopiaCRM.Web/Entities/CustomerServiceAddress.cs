using System.ComponentModel.DataAnnotations;

namespace CleantopiaCRM.Web.Entities;

public class CustomerServiceAddress
{
    public int Id { get; set; }

    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public int AddressId { get; set; }
    public Address? Address { get; set; }

    [Required, MaxLength(250)]
    public string ContactName { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? ContactPhone { get; set; }

    [MaxLength(250)]
    public string? ContactEmail { get; set; }

    [MaxLength(250)]
    public string? SiteName { get; set; }

    public bool IsDefault { get; set; }

    public bool HasOwnInvoiceInfo { get; set; }

    [MaxLength(250)]
    public string? InvoiceCompanyName { get; set; }

    [MaxLength(50)]
    public string? InvoiceTaxCode { get; set; }

    [MaxLength(500)]
    public string? InvoiceAddress { get; set; }

    [MaxLength(250)]
    public string? InvoiceEmail { get; set; }

    [MaxLength(20)]
    public string? InvoicePhone { get; set; }

    [MaxLength(250)]
    public string? InvoiceReceiver { get; set; }

    public bool IsActive { get; set; } = true;
}
