namespace CleantopiaCRM.Web.Entities;

public class RoleMenu
{
    public int Id { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public int MenuItemId { get; set; }
    public MenuItem? MenuItem { get; set; }
}
