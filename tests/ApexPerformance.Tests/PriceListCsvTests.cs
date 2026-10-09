using ApexPerformance.API.Features.PriceLists;

namespace ApexPerformance.Tests;

public class PriceListCsvTests
{
    private static PriceListRow Row(string name = "ReViv PLUS 60 kapsula", decimal price = 33.15m) =>
        new(name, "RVP-60", "ReViv PLUS", 60, "kom", price, null, 31.9m, 33.15m, "3850000000001",
            "Dodaci prehrani");

    [Fact]
    public void Build_WritesHeaderWithAnchorDate()
    {
        var csv = PriceListCsv.Build(new[] { Row() }, "10.9.2026.");

        var header = csv.Split("\r\n")[0];

        Assert.Contains("SIDRENA CIJENA NA 10.9.2026.", header);
        Assert.Equal(12, header.Split(';').Length);
    }

    [Fact]
    public void Build_WritesPricesWithDecimalCommaAndUnitPrice()
    {
        var csv = PriceListCsv.Build(new[] { Row() }, "10.9.2026.");

        Assert.Equal(
            "ReViv PLUS 60 kapsula;RVP-60;ReViv PLUS;60;kom;33,15;0,55;;31,90;33,15;3850000000001;Dodaci prehrani",
            csv.Split("\r\n")[1]);
    }

    [Fact]
    public void Build_QuotesValuesWithSeparatorOrQuotes()
    {
        var csv = PriceListCsv.Build(new[] { Row("Paket; \"veliki\"") }, "10.9.2026.");

        Assert.StartsWith("\"Paket; \"\"veliki\"\"\";", csv.Split("\r\n")[1]);
    }

    [Fact]
    public void BuildFileName_UsesDateAndTimeOfPublishing()
    {
        var publishedAt = new DateTimeOffset(2026, 11, 1, 7, 30, 0, TimeSpan.FromHours(1));

        Assert.Equal("INTERNET_TRGOVINA_REVIVPLUS_01112026_0730.csv",
            PriceListCsv.BuildFileName("INTERNET_TRGOVINA_REVIVPLUS", publishedAt));
    }
}
