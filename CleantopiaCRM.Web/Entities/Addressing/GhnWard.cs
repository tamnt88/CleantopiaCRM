namespace CleantopiaCRM.Web.Entities;

public class GhnWard
{
    public int Id { get; set; }
    public string WardCode { get; set; } = string.Empty;
    public int WardIdV2 { get; set; }
    public string WardName { get; set; } = string.Empty;
    public int ProvinceId { get; set; }
    public GhnProvince? Province { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime SyncedAt { get; set; } = DateTime.UtcNow;
}
