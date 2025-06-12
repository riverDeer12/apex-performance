namespace ApexPerformance.API.Database.Entities;

public class CoachClient
{
    public Guid CoachId { get; set; }
    
    public Coach Coach { get; set; }
    
    public Guid ClientId { get; set; }
    
    public Client Client { get; set; }
}