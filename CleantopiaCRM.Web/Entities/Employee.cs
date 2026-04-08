namespace CleantopiaCRM.Web.Entities;

public class Employee
{
    public int Id { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Email { get; set; }
    public int AddressId { get; set; }
    public Address? Address { get; set; }
    public string Role { get; set; } = "KyThuat";
    public bool IsActive { get; set; } = true;
}
