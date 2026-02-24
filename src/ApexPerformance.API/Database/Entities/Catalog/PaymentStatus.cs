using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ApexPerformance.API.Database.Entities.Catalog;

public class PaymentStatus
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Key]
    public Guid Id { get; set;}
    public required string Name { get; set; }
    public required string Description { get; set; }
    public ICollection<Payment> Payments { get; set; } = null!;
    
    public static PaymentStatus Init(string id, string name, string description)
        => new()
        {
            Id = new Guid(id),
            Name = name,
            Description = description,
        };
}