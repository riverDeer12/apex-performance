using System.ComponentModel.DataAnnotations.Schema;

namespace ApexPerformance.API.Database.Entities;

public class Client : UserType
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    public string Phone { get; set; }
    
    public ICollection<ClientAppointment> Appointments { get; set; }

    [NotMapped] public string FullName => $"{FirstName} {LastName}";
}