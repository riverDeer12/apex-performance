namespace ApexPerformance.API.Database.Entities;

public class ClientAppointment
{
    public Guid ClientId { get; set; }
    
    public Client Client { get; set; }
    
    public Guid AppointmentId { get; set; }
    
    public Appointment Appointment { get; set; }
}