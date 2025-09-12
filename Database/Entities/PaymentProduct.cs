namespace ApexPerformance.API.Database.Entities;

public class PaymentProduct
{
    public Guid PaymentId { get; set; }

    public Payment Payment { get; set; } = null!;

    public Guid ProductId { get; set; }

    public Product Product { get; set; } = null!;
}