namespace CleantopiaCRM.Web.Entities;

public class MaintenanceReminder
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public int CycleDays { get; set; } = 90;
    public DateTime LastServiceDate { get; set; }
    public DateTime NextReminderDate { get; set; }
    public bool IsDone { get; set; }
}
