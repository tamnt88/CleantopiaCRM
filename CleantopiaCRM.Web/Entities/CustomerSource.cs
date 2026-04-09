using System.ComponentModel.DataAnnotations;

namespace CleantopiaCRM.Web.Entities;

public class CustomerSource
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }

    public ICollection<Customer> Customers { get; set; } = new List<Customer>();
}
