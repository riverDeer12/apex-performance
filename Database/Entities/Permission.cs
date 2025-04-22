using System.ComponentModel.DataAnnotations;

namespace ApexPerformance.API.Database.Entities;

public class Permission
{
    [Key]
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string Category { get; set; }
    
    public ICollection<RolePermission> Roles { get; set; }
    
    public static Permission Init(string id, string name, string description, string feature)
        => new()
        {
            Id = new Guid(id),
            Name = name,
            Description = description,
            Category = feature
        };
}