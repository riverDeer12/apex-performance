using ApexPerformance.API.Database.Entities.Abstract;

namespace ApexPerformance.API.Database.Entities;

/// <summary>
/// Place where trainings are held. Coordinates and
/// Google Maps link are used for navigation to it.
/// </summary>
public class TrainingLocation : BaseEntity
{
    public required string Name { get; set; }
    public string? Address { get; set; }
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public string? GoogleMapsUrl { get; set; }
    public ICollection<Appointment> Appointments { get; set; } = null!;
}
