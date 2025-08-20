using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ApexPerformance.API.Database.Entities.Catalog;

public class TimeSlot
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Key]
    public Guid Id { get; set; }
    public DayOfWeek Day { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public ICollection<CoachTimeSlot> Coaches { get; set; } = null!;
    public ICollection<RecurringAppointment> RecurringAppointments { get; set; } = null!;
    
    [NotMapped] public string Name => $"{StartTime} - {EndTime}";
}