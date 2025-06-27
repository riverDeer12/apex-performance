using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Services;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Dashboard;

public record GetClientReportResponse(
    decimal Credits
);

public class GetClientReportEndpoint : EndpointWithoutRequest<GetClientReportResponse>
{
    private readonly ApexPerformanceContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetClientReportEndpoint(ICurrentUserService currentUserService, ApexPerformanceContext context)
    {
        _currentUserService = currentUserService;
        _context = context;
    }

    public override void Configure()
    {
        Get("api/statistics/client-report");
        Options(x => x.WithTags("Statistics"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var client = await _context.Clients.FirstOrDefaultAsync(x => x.UserId == _currentUserService.UserId,
            cancellationToken: cancellationToken);

        if (client is null)
            ThrowError(ErrorMessages.NotFound);

        await SendAsync(new GetClientReportResponse(client.Credits), cancellation: cancellationToken);
    }
}