namespace ApexPerformance.API.Database.Entities;

public class CoachAppointment
{
    public Guid CoachId { get; set; }

    public Coach Coach { get; set; } = null!;

    public Guid AppointmentId { get; set; }

    public Appointment Appointment { get; set; } = null!;
}