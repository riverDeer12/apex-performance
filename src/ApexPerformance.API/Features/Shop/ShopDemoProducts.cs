using System.Net;
using System.Text;
using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Database.Entities;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Shop;

public record ShopDemoProductsResponse(int Count);

/// <summary>
/// Invented products to see how the shop looks with a full catalog. Only on
/// test and development; their slugs start with "demo-" so they can be removed.
/// </summary>
internal static class ShopDemoProducts
{
    public const string SlugPrefix = "demo-";

    private record Kind(string Name, string Category, decimal Price, string[] Sizes, string Description, string Shape);

    private static readonly Kind[] Kinds =
    [
        new("Majica", "Majice", 25, ["XS", "S", "M", "L", "XL"], "Pamučna majica ravnog kroja za trening i svaki dan. 100 % pamuk, 180 g/m². Prati na 30 °C, ne sušiti u sušilici.", "tshirt"),
        new("Majica Dry-Fit", "Majice", 29, ["S", "M", "L", "XL"], "Lagana majica od tkanine koja brzo upija i suši znoj. 100 % poliester. Prati na 30 °C.", "tshirt"),
        new("Majica bez rukava", "Majice", 22, ["S", "M", "L", "XL"], "Široki otvori za ruke, za trening po vrućini. 95 % pamuk, 5 % elastan.", "tank"),
        new("Hoodica", "Hoodice", 55, ["S", "M", "L", "XL", "XXL"], "Topla hoodica s kapuljačom i džepom sprijeda. 80 % pamuk, 20 % poliester, unutra mekano brušena.", "hoodie"),
        new("Hoodica na zip", "Hoodice", 59, ["S", "M", "L", "XL"], "Hoodica s patentnim zatvaračem, za zagrijavanje i put do studija. 80 % pamuk, 20 % poliester.", "hoodie"),
        new("Tajice", "Tajice i hlače", 42, ["XS", "S", "M", "L"], "Tajice visokog struka koje ne prozirne u čučnju. 75 % poliamid, 25 % elastan, džep sa strane.", "leggings"),
        new("Kratke hlače", "Tajice i hlače", 32, ["S", "M", "L", "XL"], "Lagane kratke hlače s unutarnjim slojem i džepom za ključeve. 100 % poliester.", "shorts"),
        new("Trenirka hlače", "Tajice i hlače", 45, ["S", "M", "L", "XL"], "Hlače suženog kroja s elastičnim nogavicama. 80 % pamuk, 20 % poliester.", "leggings"),
        new("Sportski grudnjak", "Majice", 34, ["XS", "S", "M", "L"], "Srednja potpora, široka traka ispod grudi. 80 % poliamid, 20 % elastan.", "tank"),
        new("Čarape (3 para)", "Dodaci", 15, ["35-38", "39-42", "43-46"], "Sportske čarape s ojačanom petom i prstima, pakiranje od tri para.", "socks"),
        new("Kapa", "Dodaci", 20, [], "Kapa sa šiltom i izvezenim Apex logom, podesiva straga.", "cap"),
        new("Zimska kapa", "Dodaci", 18, [], "Pletena kapa s Apex logom.", "cap"),
        new("Ručnik", "Dodaci", 19, [], "Brzosušeći ručnik od mikrovlakana, 40 × 80 cm, za trening.", "towel"),
        new("Boca za vodu", "Dodaci", 16, [], "Boca od 750 ml bez BPA, ne propušta, s Apex logom.", "bottle"),
        new("Sportska torba", "Dodaci", 39, [], "Torba od 30 l s odvojenim pretincem za tenisice.", "bag"),
        new("Guma za istezanje", "Oprema za istezanje", 14, [], "Duga elastična guma za istezanje i mobilnost, 208 cm. Lagani otpor.", "band"),
        new("Mini band set", "Oprema za istezanje", 18, [], "Set od pet kratkih guma različitog otpora, za aktivaciju i zagrijavanje.", "band"),
        new("Traka za istezanje", "Oprema za istezanje", 12, [], "Neelastična traka s petljama za istezanje stražnje lože i ramena.", "band"),
        new("Foam roller", "Oprema za istezanje", 28, [], "Valjak od tvrde pjene za opuštanje mišića, 33 cm.", "roller"),
        new("Masažna loptica", "Oprema za istezanje", 9, [], "Tvrda loptica za masažu stopala, ramena i leđa.", "ball"),
    ];

    private static readonly (string Name, string Hex)[] Colors =
    [
        ("Crna", "#1b1f29"), ("Bijela", "#f2f2f2"), ("Plava", "#0034C4"), ("Siva", "#8a8f99"),
        ("Tamnozelena", "#1f4d3a"), ("Bordo", "#6b1f2e"), ("Pijesak", "#d8c7a4"),
    ];

    private static readonly string[] Lines = ["Core", "Performance", "Studio", "Essential", "Pro"];

    public static List<ShopProduct> Create(int count)
    {
        var random = new Random(2026);
        var products = new List<ShopProduct>();

        for (var i = 0; products.Count < count; i++)
        {
            var kind = Kinds[i % Kinds.Length];
            var line = Lines[i / Kinds.Length % Lines.Length];
            var colors = Colors.OrderBy(_ => random.Next()).Take(kind.Sizes.Length > 0 ? random.Next(1, 4) : 1).ToList();
            var name = $"{kind.Name} Apex {line}" + (colors.Count == 1 && kind.Sizes.Length > 0 ? $" – {colors[0].Name.ToLowerInvariant()}" : "");

            var product = new ShopProduct
            {
                Name = name,
                Slug = SlugPrefix + ShopSettings.ToSlug(name) + "-" + i,
                Description = kind.Description,
                Category = kind.Category,
                Price = kind.Price + (line == "Pro" ? 10 : line == "Performance" ? 5 : 0),
                IsActive = true,
                SortOrder = 100 + i
            };

            var sortOrder = 0;
            foreach (var color in colors)
            {
                foreach (var size in kind.Sizes.Length > 0 ? kind.Sizes : [null])
                {
                    product.Variants.Add(new ShopProductVariant
                    {
                        Size = size,
                        Color = kind.Sizes.Length > 0 && colors.Count > 1 ? color.Name : null,
                        // Some sold out and some nearly, to see how the shop shows them.
                        Stock = random.Next(10) switch { 0 => 0, 1 => random.Next(1, 4), _ => random.Next(5, 40) },
                        SortOrder = sortOrder++
                    });
                }

                product.Images.Add(new ShopProductImage
                {
                    ContentType = "image/svg+xml",
                    Data = Encoding.UTF8.GetBytes(Picture(kind.Shape, color.Hex, kind.Name, line)),
                    SortOrder = product.Images.Count,
                    CreatedAt = DateTimeOffset.UtcNow
                });
            }

            products.Add(product);
        }

        return products;
    }

    // Simple drawing of the product in its color, so the catalog looks like one.
    private static string Picture(string shape, string color, string name, string line)
    {
        var stroke = color is "#f2f2f2" or "#d8c7a4" ? "#c9ccd3" : "none";
        var figure = shape switch
        {
            "tshirt" => "M120 90 L170 70 Q200 90 230 70 L280 90 L320 150 L280 170 L270 150 L270 330 L130 330 L130 150 L120 170 L80 150 Z",
            "tank" => "M150 70 Q160 120 200 125 Q240 120 250 70 L275 80 Q265 140 270 160 L270 330 L130 330 L130 160 Q135 140 125 80 Z",
            "hoodie" => "M150 80 Q200 30 250 80 L300 100 L330 230 L295 240 L280 170 L280 330 L120 330 L120 170 L105 240 L70 230 L100 100 Z",
            "leggings" => "M140 70 L260 70 L270 330 L215 330 L200 150 L185 330 L130 330 Z",
            "shorts" => "M130 110 L270 110 L285 250 L215 260 L200 170 L185 260 L115 250 Z",
            "socks" => "M170 70 L230 70 L230 230 Q230 290 170 300 L120 300 Q95 290 110 260 L170 230 Z",
            "cap" => "M100 220 Q100 110 200 110 Q300 110 300 220 Z M290 210 L350 230 L300 240 Z",
            "towel" => "M110 100 L290 100 L290 320 L110 320 Z",
            "bottle" => "M180 60 L220 60 L220 100 Q260 120 260 160 L260 330 L140 330 L140 160 Q140 120 180 100 Z",
            "bag" => "M90 160 L310 160 L300 310 L100 310 Z M150 160 Q150 100 200 100 Q250 100 250 160",
            "band" => "M200 90 C320 90 320 310 200 310 C80 310 80 90 200 90 Z M200 120 C280 120 280 280 200 280 C120 280 120 120 200 120 Z",
            "roller" => "M110 150 L290 150 Q320 200 290 250 L110 250 Q80 200 110 150 Z",
            _ => "M200 120 A80 80 0 1 1 199 120 Z"
        };

        string E(string value) => WebUtility.HtmlEncode(value);

        return "<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 400 500\">" +
               "<rect width=\"400\" height=\"500\" fill=\"#eceff5\"/>" +
               $"<path d=\"{figure}\" fill=\"{color}\" fill-rule=\"evenodd\" stroke=\"{stroke}\" stroke-width=\"3\"/>" +
               $"<text x=\"200\" y=\"420\" text-anchor=\"middle\" font-family=\"Arial\" font-size=\"22\" fill=\"#5b6475\">{E(name)}</text>" +
               $"<text x=\"200\" y=\"452\" text-anchor=\"middle\" font-family=\"Arial\" font-size=\"16\" letter-spacing=\"4\" fill=\"#0034C4\">APEX {E(line.ToUpperInvariant())}</text>" +
               "</svg>";
    }
}

/// <summary>
/// Adds invented products for trying the shop with a full catalog. Refused on production.
/// </summary>
public class CreateShopDemoProductsEndpoint : EndpointWithoutRequest<ShopDemoProductsResponse>
{
    private const int Count = 100;

    private readonly ApexPerformanceContext _context;
    private readonly IHostEnvironment _environment;

    public CreateShopDemoProductsEndpoint(ApexPerformanceContext context, IHostEnvironment environment)
    {
        _context = context;
        _environment = environment;
    }

    public override void Configure()
    {
        Post("api/shop/admin/demo-products");
        Roles(ShopAdmin.Roles);
        Options(x => x.WithTags("Shop"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        if (_environment.IsProduction())
            ThrowError(ErrorCodes.UnauthorizedAction, StatusCodes.Status403Forbidden);

        // Once at a time: the ones already there are removed first.
        if (await _context.ShopProducts.AnyAsync(
                x => x.Slug.StartsWith(ShopDemoProducts.SlugPrefix) && !x.IsDeleted, cancellationToken))
            ThrowError(ErrorCodes.AlreadyExists);

        var products = ShopDemoProducts.Create(Count);
        _context.ShopProducts.AddRange(products);
        await _context.SaveChangesAsync(cancellationToken);

        await SendAsync(new ShopDemoProductsResponse(products.Count), cancellation: cancellationToken);
    }
}

/// <summary>
/// Removes the invented products like a normal delete; orders made with them keep them.
/// </summary>
public class DeleteShopDemoProductsEndpoint : EndpointWithoutRequest<ShopDemoProductsResponse>
{
    private readonly ApexPerformanceContext _context;
    private readonly IHostEnvironment _environment;

    public DeleteShopDemoProductsEndpoint(ApexPerformanceContext context, IHostEnvironment environment)
    {
        _context = context;
        _environment = environment;
    }

    public override void Configure()
    {
        Delete("api/shop/admin/demo-products");
        Roles(ShopAdmin.Roles);
        Options(x => x.WithTags("Shop"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        if (_environment.IsProduction())
            ThrowError(ErrorCodes.UnauthorizedAction, StatusCodes.Status403Forbidden);

        var products = await _context.ShopProducts
            .Where(x => x.Slug.StartsWith(ShopDemoProducts.SlugPrefix) && !x.IsDeleted)
            .ToListAsync(cancellationToken);

        foreach (var product in products)
        {
            product.Delete();
            product.IsActive = false;
            product.Slug = $"deleted-{product.Id:N}";
        }

        await _context.SaveChangesAsync(cancellationToken);

        await SendAsync(new ShopDemoProductsResponse(products.Count), cancellation: cancellationToken);
    }
}
