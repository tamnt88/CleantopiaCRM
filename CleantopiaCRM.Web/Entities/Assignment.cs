namespace CleantopiaCRM.Web.Entities;

public class Assignment
{
    public int Id { get; set; }
    public int AppointmentId { get; set; }
    public Appointment? Appointment { get; set; }
    public int EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public string Role { get; set; } = "Thuc hien";
    public string? SupervisionNote { get; set; }
}
