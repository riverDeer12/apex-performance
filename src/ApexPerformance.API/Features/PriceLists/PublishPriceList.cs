using ApexPerformance.API.Constants;
using ApexPerformance.API.Services.Implementation;
using ApexPerformance.API.Services.Interfaces;
using FastEndpoints;

namespace ApexPerformance.API.Features.PriceLists;

/// <summary>
/// Publish the price list right away, e.g. after setting up
/// products or when the scheduled publish failed.
/// </summary>
public class PublishPriceListEndpoint : EndpointWithoutRequest<PriceListFileResponse>
{
    private readonly IPriceListService _priceListService;

    public PublishPriceListEndpoint(IPriceListService priceListService)
    {
        _priceListService = priceListService;
    }

    public override void Configure()
    {
        Post("api/price-lists/reviv-plus/publish");
        Roles(UserRoles.SuperAdmin, UserRoles.Administrator);
        Options(x => x.WithTags("PriceLists"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var file = await _priceListService.PublishAsync(PriceListPublishReasons.Manual);

        var baseUrl = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}";

        await SendAsync(new PriceListFileResponse(file.FileName, file.PublishedAt, file.Reason,
                $"{baseUrl}/api/price-lists/reviv-plus/files/{Uri.EscapeDataString(file.FileName)}"),
            cancellation: cancellationToken);
    }
}
