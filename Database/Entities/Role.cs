namespace ApexPerformance.API.Database.Entities;

public class Role : BaseEntity
{
    public string Name { get; set; }

    public string Description { get; set; }

    public ICollection<UserRole> Users { get; set; }
    public ICollection<RolePermission> Permissions { get; set; }

    public static Role Init(string roleName, string description)
        => new()
        {
            Name = roleName,
            Description = description
        };
}