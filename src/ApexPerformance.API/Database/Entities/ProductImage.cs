using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ApexPerformance.API.Database.Entities;

public class ProductImage
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Key]
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Source { get; set; } 
    public bool IsActive { get; set; } 
    public Guid ProductId { get; set; }
    public required Product Product { get; set; }
}