namespace CleantopiaCRM.Web.Entities;

public class AppUser
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = "Admin";
    public int? EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public bool IsActive { get; set; } = true;
}
