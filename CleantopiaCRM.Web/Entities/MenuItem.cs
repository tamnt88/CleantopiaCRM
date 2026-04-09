namespace CleantopiaCRM.Web.Entities;

public class MenuItem
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Url { get; set; }
    public string? IconCss { get; set; }
    public int? ParentId { get; set; }
    public MenuItem? Parent { get; set; }
    public ICollection<MenuItem> Children { get; set; } = new List<MenuItem>();
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}
