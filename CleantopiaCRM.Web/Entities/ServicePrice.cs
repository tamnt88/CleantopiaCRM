using System.ComponentModel.DataAnnotations;

namespace CleantopiaCRM.Web.Entities;

public class ServicePrice
{
    public int Id { get; set; }
    [Required, MaxLength(100)]
    public string Category { get; set; } = "Máy lạnh";
    [Required, MaxLength(250)]
    public string ServiceName { get; set; } = string.Empty;
    [MaxLength(100)]
    public string? VariantName { get; set; }
    [MaxLength(100)]
    public string Unit { get; set; } = "Goi";
    public decimal Price { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; }
}
