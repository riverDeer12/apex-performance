using ApexPerformance.API.Database.Entities.Abstract;

namespace ApexPerformance.API.Database.Entities;

public class Role : BaseEntity
{
    public required string Name { get; set; }

    public required string Description { get; set; }

    public ICollection<UserRole> Users { get; set; } = null!;
    public ICollection<RolePermission> Permissions { get; set; } = null!;

    public static Role Init(string roleName, string description)
        => new()
        {
            Name = roleName,
            Description = description
        };
}