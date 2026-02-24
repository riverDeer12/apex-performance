using ApexPerformance.API.Database.Entities.Abstract;

namespace ApexPerformance.API.Database.Entities;

public class Product : BaseEntity
{
    public required string Name { get; set; }
    public required string Description { get; set; }
    public decimal Price { get; set; }
    public bool IsActive { get; set; }
    public ICollection<PaymentProduct> Payments  { get; set; } = null!;
    
    public ICollection<ProductImage> ProductImages  { get; set; } = null!;
}