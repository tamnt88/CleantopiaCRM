namespace CleantopiaCRM.Web.Entities;

public class GhnProvince
{
    public int Id { get; set; }
    public int ProvinceId { get; set; }
    public string ProvinceName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime SyncedAt { get; set; } = DateTime.UtcNow;
    public ICollection<GhnWard> Wards { get; set; } = new List<GhnWard>();
}
