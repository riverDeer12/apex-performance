namespace ApexPerformance.API.Database.Entities;

public class ClientRecurringAppointment
{
    public Guid ClientId { get; set; }

    public Client Client { get; set; } = null!;

    public Guid RecurringAppointmentId { get; set; }

    public RecurringAppointment RecurringAppointment { get; set; } = null!;
}