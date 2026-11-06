using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ApexPerformance.API.Database.Entities;

/// <summary>
/// Product a logged in member of the shop marked with the heart, so their
/// favorites are the same on every device. Visitors who are not logged in
/// keep them in the browser only.
/// </summary>
public class ShopFavorite
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Key]
    public Guid Id { get; set; }

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public Guid ProductId { get; set; }
    public ShopProduct Product { get; set; } = null!;

    // Size and color chosen when it was marked, if any.
    public string? Size { get; set; }
    public string? Color { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
