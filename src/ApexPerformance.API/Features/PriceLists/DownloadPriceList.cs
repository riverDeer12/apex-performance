using System.Text;
using ApexPerformance.API.Database;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.PriceLists;

/// <summary>
/// Price list file exactly as it was published.
/// </summary>
public class DownloadPriceListEndpoint : EndpointWithoutRequest
{
    private readonly ApexPerformanceContext _context;

    public DownloadPriceListEndpoint(ApexPerformanceContext context)
    {
        _context = context;
    }

    public override void Configure()
    {
        Get("api/price-lists/reviv-plus/files/{fileName}");
        AllowAnonymous();
        Options(x => x.WithTags("PriceLists"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var fileName = Route<string>("fileName", isRequired: true);

        var file = await _context.PriceListFiles
            .FirstOrDefaultAsync(x => x.FileName == fileName, cancellationToken);

        if (file is null)
        {
            await SendNotFoundAsync(cancellationToken);
            return;
        }

        // BOM so Excel opens the file as UTF-8 (č, ć, š, ž, đ).
        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(file.Content)).ToArray();

        await SendBytesAsync(bytes, file.FileName, "text/csv; charset=utf-8",
            cancellation: cancellationToken);
    }
}
