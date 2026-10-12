using ApexPerformance.API.Database.Entities.Abstract;

namespace ApexPerformance.API.Database.Entities;

/// <summary>
/// Price list file exactly as it was published, kept 30 days
/// so it can be downloaded from the web shop.
/// </summary>
public class PriceListFile : BaseEntity
{
    public required string FileName { get; set; }
    public required string Content { get; set; }
    public DateTimeOffset PublishedAt { get; set; }

    /// <summary>
    /// Why the file was published: daily schedule, price change in Stripe or manually.
    /// </summary>
    public required string Reason { get; set; }

    /// <summary>
    /// Current prices by Stripe product id, used for the lowest price in the last 30 days
    /// and to tell if prices changed since the last published file.
    /// </summary>
    public required string PricesJson { get; set; }
}
