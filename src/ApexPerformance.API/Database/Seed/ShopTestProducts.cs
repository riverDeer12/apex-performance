using System.Text.Json;
using System.Text.RegularExpressions;
using ApexPerformance.API.Database.Entities;
using ApexPerformance.API.Features.Shop;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Database.Seed;

/// <summary>
/// Sample products for the test shop, to see how it looks with a fuller catalog.
/// Runs once at start-up in the Test environment only; products already there
/// (by slug) are skipped. Temporary: removed once the products are in the test database.
/// </summary>
internal static class ShopTestProducts
{
    private record Item(string Slug, string Name, string Category, decimal Price, string Description,
        List<string> Colors, List<string?> Sizes, List<string> Keywords, List<string> Related);

    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(ShopTestProducts));

        try
        {
            var context = scope.ServiceProvider.GetRequiredService<ApexPerformanceContext>();
            var items = JsonSerializer.Deserialize<List<Item>>(Read("products.json"),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;

            var added = 0;
            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];
                if (await context.ShopProducts.AnyAsync(x => x.Slug == item.Slug))
                    continue;

                var product = new ShopProduct
                {
                    Name = item.Name,
                    Slug = item.Slug,
                    Description = item.Description,
                    Category = item.Category,
                    Price = item.Price,
                    IsActive = true,
                    SortOrder = 100 + i,
                    Keywords = item.Keywords,
                };

                var order = 0;
                foreach (var color in item.Colors)
                foreach (var size in item.Sizes)
                {
                    // Mostly in stock, some sizes nearly or completely sold out.
                    var stock = (i + order) % 9 == 0 ? 0 : (i + order) % 5 == 0 ? 2 : 8 + (i + order) % 7;
                    var variant = new ShopProductVariant { Size = size, Color = color, Stock = stock, SortOrder = order++ };
                    product.Variants.Add(variant);
                    ShopStock.Record(context, variant, stock, ShopStockReasons.Initial);
                }

                var picture = 0;
                foreach (var color in item.Colors)
                    product.Images.Add(new ShopProductImage
                    {
                        ContentType = "image/webp",
                        Data = ReadBytes($"{item.Slug}-{Slug(color)}.webp"),
                        SortOrder = picture++,
                        CreatedAt = DateTimeOffset.UtcNow,
                    });

                context.ShopProducts.Add(product);
                await context.SaveChangesAsync();
                added++;
            }

            // "Goes well with" points to products by id, known once all are saved.
            var slugs = items.Select(x => x.Slug).ToList();
            var bySlug = await context.ShopProducts
                .Where(x => slugs.Contains(x.Slug))
                .ToDictionaryAsync(x => x.Slug);
            foreach (var item in items.Where(x => x.Related.Count > 0))
                if (bySlug.TryGetValue(item.Slug, out var product) && product.RelatedProductIds.Count == 0)
                    product.RelatedProductIds = item.Related.Where(bySlug.ContainsKey).Select(x => bySlug[x].Id).ToList();
            await context.SaveChangesAsync();

            logger.LogInformation("Test shop sample products: {Added} added, {Total} in the list", added, items.Count);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Test shop sample products were not added");
        }
    }

    private static string Slug(string text)
        => Regex.Replace(text.ToLowerInvariant()
            .Replace('č', 'c').Replace('ć', 'c').Replace('š', 's').Replace('ž', 'z').Replace("đ", "d"),
            "[^a-z0-9]+", "-").Trim('-');

    private static Stream Open(string name)
        => typeof(ShopTestProducts).Assembly.GetManifestResourceStream("ShopTestProducts/" + name)
           ?? throw new FileNotFoundException(name);

    private static string Read(string name)
    {
        using var reader = new StreamReader(Open(name));
        return reader.ReadToEnd();
    }

    private static byte[] ReadBytes(string name)
    {
        using var stream = Open(name);
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        return memory.ToArray();
    }
}
