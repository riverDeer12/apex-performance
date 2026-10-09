using ApexPerformance.API.Database;
using ApexPerformance.API.Services.Implementation;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.PriceLists;

public record PriceListFileResponse(string FileName, DateTimeOffset PublishedAt, string Reason, string Url);

/// <summary>
/// Price lists of the last 30 days, shown on reviv-plus.com/cjenici
/// and used by tools that download price lists automatically.
/// </summary>
public class GetPriceListsEndpoint : EndpointWithoutRequest<List<PriceListFileResponse>>
{
    private readonly ApexPerformanceContext _context;

    public GetPriceListsEndpoint(ApexPerformanceContext context)
    {
        _context = context;
    }

    public override void Configure()
    {
        Get("api/price-lists/reviv-plus");
        AllowAnonymous();
        Options(x => x.WithTags("PriceLists"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var baseUrl = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}";

        var from = DateTimeOffset.UtcNow.AddDays(-PriceListService.RetentionDays);

        var files = await _context.PriceListFiles
            .Where(x => x.PublishedAt >= from)
            .OrderByDescending(x => x.PublishedAt)
            .Select(x => new { x.FileName, x.PublishedAt, x.Reason })
            .ToListAsync(cancellationToken);

        await SendAsync(files.Select(x => new PriceListFileResponse(x.FileName, x.PublishedAt, x.Reason,
                $"{baseUrl}/api/price-lists/reviv-plus/files/{Uri.EscapeDataString(x.FileName)}"))
            .ToList(), cancellation: cancellationToken);
    }
}
