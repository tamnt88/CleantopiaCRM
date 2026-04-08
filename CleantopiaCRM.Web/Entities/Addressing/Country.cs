namespace CleantopiaCRM.Web.Entities;

public class Country
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public ICollection<Customer> Customers { get; set; } = new List<Customer>();
}
