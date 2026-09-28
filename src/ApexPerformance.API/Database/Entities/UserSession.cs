using ApexPerformance.API.Database.Entities.Abstract;

namespace ApexPerformance.API.Database.Entities;

/// <summary>
/// One successful login of a user.
/// Login time is stored in CreatedAt.
/// </summary>
public class UserSession : BaseEntity
{
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public bool RememberMe { get; set; }
}
