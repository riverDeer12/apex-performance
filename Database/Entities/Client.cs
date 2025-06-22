using System.ComponentModel.DataAnnotations.Schema;
using ApexPerformance.API.Database.Entities.Abstract;

namespace ApexPerformance.API.Database.Entities;

public class Client : UserType
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    public string Phone { get; set; }
    public int Credits { get; set; }
    public ICollection<ClientAppointment> Appointments { get; set; }
    
    public ICollection<BodyMeasurement> BodyMeasurements { get; set; }
    public ICollection<CoachClient> Coaches { get; set; }
    
    [NotMapped] public string FullName => $"{FirstName} {LastName}";
}