using FastEndpoints;

namespace ApexPerformance.API.Features.Dashboard;

public record GetStatisticsResponse();

public class GetStatisticsEndpoint : EndpointWithoutRequest<GetStatisticsResponse>
{
    public override void Configure()
    {
        base.Configure();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        await base.HandleAsync(ct);
    }
}