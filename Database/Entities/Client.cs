using System.ComponentModel.DataAnnotations.Schema;
using ApexPerformance.API.Database.Entities.Abstract;

namespace ApexPerformance.API.Database.Entities;

public class Client : UserType
{
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string Email { get; set; }
    public required string Phone { get; set; }
    public int Credits { get; set; }
    public ICollection<ClientAppointment> Appointments { get; set; } = null!;
    
    public ICollection<BodyMeasurement> BodyMeasurements { get; set; } = null!;
    public ICollection<CoachClient> Coaches { get; set; } = null!;
    public ICollection<ClientRecurringAppointment> RecurringAppointments { get; set; } = null!;
    
    [NotMapped] public string FullName => $"{FirstName} {LastName}";
}