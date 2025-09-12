using ApexPerformance.API.Database.Entities.Abstract;
using ApexPerformance.API.Database.Entities.Catalog;

namespace ApexPerformance.API.Database.Entities;

public class Payment : BaseEntity
{
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public string CustomerFirstName { get; set; }
    public string CustomerLastName { get; set; }
    public string CustomerPhone { get; set; }
    public string CustomerEmail { get; set; }
    public string CustomerAddress { get; set; }
    public PaymentType PaymentType { get; set; }
    public Guid PaymentTypeId { get; set; }
    public PaymentStatus PaymentStatus { get; set; }
    public Guid PaymentStatusId { get; set; }
    public ICollection<PaymentProduct> Products  { get; set; } = null!;
}