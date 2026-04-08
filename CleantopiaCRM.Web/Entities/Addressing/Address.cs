namespace CleantopiaCRM.Web.Entities;

public class Address
{
    public int Id { get; set; }
    public string HouseNumber { get; set; } = string.Empty;
    public string Street { get; set; } = string.Empty;
    public int ProvinceId { get; set; }
    public GhnProvince? Province { get; set; }
    public int WardId { get; set; }
    public GhnWard? Ward { get; set; }
    public string FullText => $"{HouseNumber} {Street}, {Ward?.WardName}, {Province?.ProvinceName}";
}
