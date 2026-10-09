using System.Globalization;
using System.Text;

namespace ApexPerformance.API.Features.PriceLists;

/// <summary>
/// One product row of the price list.
/// </summary>
public record PriceListRow(
    string Name,
    string Code,
    string Brand,
    decimal NetQuantity,
    string UnitOfMeasure,
    decimal Price,
    decimal? SpecialSalePrice,
    decimal LowestPriceLast30Days,
    decimal AnchorPrice,
    string? Barcode,
    string Category);

/// <summary>
/// Builds the price list CSV and its file name. Columns follow the format
/// used by retail chains since 2025; final columns and file name scheme
/// must be checked against the notice from the client's accountant.
/// </summary>
public static class PriceListCsv
{
    private const char Separator = ';';

    public static string BuildFileName(string prefix, DateTimeOffset publishedAtLocal)
        => $"{prefix}_{publishedAtLocal:ddMMyyyy}_{publishedAtLocal:HHmm}.csv";

    public static string Build(IEnumerable<PriceListRow> rows, string anchorDate)
    {
        var csv = new StringBuilder();

        AppendLine(csv,
            "NAZIV PROIZVODA",
            "ŠIFRA PROIZVODA",
            "MARKA PROIZVODA",
            "NETO KOLIČINA",
            "JEDINICA MJERE",
            "MALOPRODAJNA CIJENA",
            "CIJENA ZA JEDINICU MJERE",
            "MPC ZA VRIJEME POSEBNOG OBLIKA PRODAJE",
            "NAJNIŽA CIJENA U POSLJEDNIH 30 DANA",
            $"SIDRENA CIJENA NA {anchorDate}",
            "BARKOD",
            "KATEGORIJA PROIZVODA");

        foreach (var row in rows)
        {
            var unitPrice = row.NetQuantity > 0 ? row.Price / row.NetQuantity : row.Price;

            AppendLine(csv,
                row.Name,
                row.Code,
                row.Brand,
                FormatQuantity(row.NetQuantity),
                row.UnitOfMeasure,
                FormatPrice(row.Price),
                FormatPrice(unitPrice),
                row.SpecialSalePrice is null ? "" : FormatPrice(row.SpecialSalePrice.Value),
                FormatPrice(row.LowestPriceLast30Days),
                FormatPrice(row.AnchorPrice),
                row.Barcode ?? "",
                row.Category);
        }

        return csv.ToString();
    }

    /// <summary>
    /// Prices with a decimal comma, as Croatian Excel expects.
    /// </summary>
    public static string FormatPrice(decimal price)
        => Math.Round(price, 2, MidpointRounding.AwayFromZero)
            .ToString("0.00", CultureInfo.InvariantCulture)
            .Replace('.', ',');

    private static string FormatQuantity(decimal quantity)
        => quantity.ToString("0.###", CultureInfo.InvariantCulture).Replace('.', ',');

    private static void AppendLine(StringBuilder csv, params string[] values)
    {
        csv.Append(string.Join(Separator, values.Select(Escape)));
        csv.Append("\r\n");
    }

    private static string Escape(string value)
    {
        if (value.IndexOfAny(new[] { Separator, '"', '\r', '\n' }) < 0)
            return value;

        return $"\"{value.Replace("\"", "\"\"")}\"";
    }
}
