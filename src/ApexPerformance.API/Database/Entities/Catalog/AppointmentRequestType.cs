using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ApexPerformance.API.Database.Entities.Catalog;

public class AppointmentRequestType
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Key]
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public ICollection<AppointmentRequest> AppointmentsRequests { get; set; } = null!;
}