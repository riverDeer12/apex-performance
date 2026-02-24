using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ApexPerformance.API.Database.Entities.Catalog;

public class AppointmentType
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Key]
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public ICollection<Appointment> Appointments { get; set; } = null!;
    
    public ICollection<RecurringAppointment> RecurringAppointments { get; set; } = null!;
}