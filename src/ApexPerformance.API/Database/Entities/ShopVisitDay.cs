namespace ApexPerformance.API.Database.Entities;

/// <summary>
/// Views of a page of the shop in a day, counted without cookies or
/// anything about the visitor, so the shop needs no consent banner.
/// </summary>
public class ShopVisitDay
{
    public DateOnly Date { get; set; }

    // "/", "/proizvod/{slug}", "/kosarica" or "/narudzba".
    public required string Path { get; set; }

    public int Count { get; set; }
}
