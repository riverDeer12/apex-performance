using ApexPerformance.API.Database.Entities.Abstract;

namespace ApexPerformance.API.Database.Entities;

public class DeviceToken : BaseEntity
{
    public string Token { get; set; }
    public string Platform { get; set; }
    public User? User { get; set; }
    public Guid UserId { get; set; }
}