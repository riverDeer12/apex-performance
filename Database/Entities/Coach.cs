using System.ComponentModel.DataAnnotations.Schema;
using ApexPerformance.API.Database.Entities.Abstract;

namespace ApexPerformance.API.Database.Entities;

public class Coach : UserType
{
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string Email { get; set; }
    public required string Phone { get; set; }
    public ICollection<CoachClient> Clients { get; set; } = null!;
    public ICollection<CoachAppointment> Appointments { get; set; } = null!;
    public ICollection<CoachTimeSlot> TimeSlots { get; set; } = null!;

    public ICollection<RecurringAppointment> RecurringAppointments { get; set; } = null!;

    [NotMapped] public string FullName => $"{FirstName} {LastName}";
}