using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ApexPerformance.API.Database.Entities;

/// <summary>
/// Picture of a shop product. Kept in the database, so the nightly backup
/// has it too. The first one by SortOrder is the main picture.
/// </summary>
public class ShopProductImage
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Key]
    public Guid Id { get; set; }

    public Guid ProductId { get; set; }
    public ShopProduct Product { get; set; } = null!;

    public required string ContentType { get; set; }
    public required byte[] Data { get; set; }

    public int SortOrder { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
