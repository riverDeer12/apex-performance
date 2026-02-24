namespace ApexPerformance.API.Database.Entities;

public class CoachClient
{
    public Guid CoachId { get; set; }
    
    public Coach Coach { get; set; } = null!;
    
    public Guid ClientId { get; set; }
    
    public Client Client { get; set; } = null!;
}