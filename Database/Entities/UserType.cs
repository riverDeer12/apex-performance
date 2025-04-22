namespace ApexPerformance.API.Database.Entities;

public abstract class UserType : BaseEntity
{
    public User? User { get; set; }
    
    public Guid UserId { get; set; }
}