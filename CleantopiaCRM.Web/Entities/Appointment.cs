namespace CleantopiaCRM.Web.Entities;

public class Appointment
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public DateTime ScheduledAt { get; set; }
    public string Type { get; set; } = "Khao sat";
    public string Status { get; set; } = "Planned";
    public string? Notes { get; set; }
}
