using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ApexPerformance.API.Database.Entities.Catalog;

public class AppointmentRequestStatus
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Key]
    public Guid Id { get; set;}
    public required string Name { get; set; }
    public required string Description { get; set; }
    public ICollection<AppointmentRequest> AppointmentRequests { get; set; } = null!;
    
    public static AppointmentRequestStatus Init(string id, string name, string description)
        => new()
        {
            Id = new Guid(id),
            Name = name,
            Description = description,
        };
}