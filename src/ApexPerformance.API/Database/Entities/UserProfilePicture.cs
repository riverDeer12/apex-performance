namespace ApexPerformance.API.Database.Entities;

/// <summary>
/// Profile picture of a user. Only the latest
/// picture is kept, uploading a new one replaces it.
/// Kept in a separate table so loading users
/// doesn't load picture bytes.
/// </summary>
public class UserProfilePicture
{
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public byte[] Content { get; set; } = [];
    public required string ContentType { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
