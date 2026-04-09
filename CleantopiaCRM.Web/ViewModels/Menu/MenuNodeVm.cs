namespace CleantopiaCRM.Web.ViewModels.Menu;

public class MenuNodeVm
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Url { get; set; }
    public string? IconCss { get; set; }
    public int SortOrder { get; set; }
    public List<MenuNodeVm> Children { get; set; } = new();
}
