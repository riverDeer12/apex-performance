using ApexPerformance.API.Database.Entities.Abstract;
using ApexPerformance.API.Database.Entities.Catalog;

namespace ApexPerformance.API.Database.Entities;

public class Payment : BaseEntity
{
    public required decimal Price { get; set; }
    public required int Quantity { get; set; }
    public required string CustomerFirstName { get; set; }
    public required string CustomerLastName { get; set; }
    public required string CustomerPhone { get; set; }
    public required string CustomerEmail { get; set; }
    public required string CustomerAddress { get; set; }
    public required PaymentType PaymentType { get; set; }
    public Guid PaymentTypeId { get; set; }
    public required PaymentStatus PaymentStatus { get; set; }
    public Guid PaymentStatusId { get; set; }
    public ICollection<PaymentProduct> Products { get; set; } = null!;
}