using System.ComponentModel.DataAnnotations;

namespace CleantopiaCRM.Web.Entities;

public class Customer
{
    public int Id { get; set; }

    [MaxLength(30)]
    public string? CustomerCode { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên khách hàng."), MaxLength(250)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? Phone { get; set; }

    [MaxLength(250)]
    public string? Email { get; set; }

    public int CountryId { get; set; }
    public Country? Country { get; set; }

    public int? CustomerSourceId { get; set; }
    public CustomerSource? CustomerSource { get; set; }

    public int? CustomerTypeId { get; set; }
    public CustomerType? CustomerType { get; set; }

    public bool IsBusiness { get; set; }

    [MaxLength(250)]
    public string? CompanyName { get; set; }

    [MaxLength(50)]
    public string? TaxCode { get; set; }

    [MaxLength(500)]
    public string? BillingAddress { get; set; }

    [MaxLength(250)]
    public string? BillingEmail { get; set; }

    [MaxLength(20)]
    public string? BillingPhone { get; set; }

    [MaxLength(250)]
    public string? BillingReceiver { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public string? Notes { get; set; }

    public ICollection<CustomerServiceAddress> ServiceAddresses { get; set; } = new List<CustomerServiceAddress>();
    public ICollection<Quote> Quotes { get; set; } = new List<Quote>();
}
