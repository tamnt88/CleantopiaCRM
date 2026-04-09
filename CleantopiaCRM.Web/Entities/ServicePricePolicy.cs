namespace CleantopiaCRM.Web.Entities;

public class ServicePricePolicy
{
    public int Id { get; set; }
    public int ServicePriceId { get; set; }
    public ServicePrice? ServicePrice { get; set; }

    public int UnitId { get; set; }
    public ServiceUnit? Unit { get; set; }

    public int RecareCycleDays { get; set; } = 180;
}
