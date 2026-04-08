using System.ComponentModel.DataAnnotations;

namespace CleantopiaCRM.Web.Entities;

public class Customer
{
    public int Id { get; set; }
    [Required, MaxLength(250)]
    public string Name { get; set; } = string.Empty;
    [MaxLength(20)]
    public string? Phone { get; set; }
    [MaxLength(250)]
    public string? Email { get; set; }
    public int CountryId { get; set; }
    public Country? Country { get; set; }
    public int AddressId { get; set; }
    public Address? Address { get; set; }
    public string? Notes { get; set; }
    public ICollection<Quote> Quotes { get; set; } = new List<Quote>();
}
