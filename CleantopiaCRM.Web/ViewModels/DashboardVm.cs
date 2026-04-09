namespace CleantopiaCRM.Web.ViewModels;

public class DashboardVm
{
    public int CustomerCount { get; set; }
    public int AppointmentToday { get; set; }
    public decimal Revenue { get; set; }
    public int PendingReminders { get; set; }

    public int NewCustomersThisMonth { get; set; }
    public int FirstPurchaseCustomersThisMonth { get; set; }
    public int ReturningCustomersThisMonth { get; set; }
    public decimal FirstPurchaseRate { get; set; }
    public decimal ReturningRate { get; set; }

    public List<SourceSummaryVm> CustomerBySource { get; set; } = new();
    public List<ServiceUsageVm> TopServices { get; set; } = new();
}

public class SourceSummaryVm
{
    public string Source { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class ServiceUsageVm
{
    public string ServiceName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal Revenue { get; set; }
}
