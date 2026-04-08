namespace CleantopiaCRM.Web.ViewModels;

public class DashboardVm
{
    public int CustomerCount { get; set; }
    public int AppointmentToday { get; set; }
    public decimal Revenue { get; set; }
    public int PendingReminders { get; set; }
}
