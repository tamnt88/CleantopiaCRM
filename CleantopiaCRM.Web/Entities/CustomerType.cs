using System.ComponentModel.DataAnnotations;

namespace CleantopiaCRM.Web.Entities;

public class CustomerType
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public bool IsBusiness { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }

    public ICollection<Customer> Customers { get; set; } = new List<Customer>();
}
