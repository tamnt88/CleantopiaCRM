using System.ComponentModel.DataAnnotations;

namespace CleantopiaCRM.Web.Entities;

public class ServiceCategory
{
    public int Id { get; set; }

    [Required, MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    public int? ParentId { get; set; }
    public ServiceCategory? Parent { get; set; }

    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
}
