namespace CleantopiaCRM.Web.Entities;

public class ServiceFeedback
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public int? AppointmentId { get; set; }
    public Appointment? Appointment { get; set; }
    public int Rating { get; set; } = 5;
    public string? Content { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
