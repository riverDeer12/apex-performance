using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Clients;

public record GetClientCreditsResponse(
    Guid Id,
    decimal Credits,
    string Firstname,
    string Lastname
);

public class GetClientCreditsEndpoint : EndpointWithoutRequest<List<GetClientCreditsResponse>>
{
    private readonly ApexPerformanceContext _context;

    public GetClientCreditsEndpoint(ApexPerformanceContext context)
    {
        _context = context;
    }

    public override void Configure()
    {
        Get("api/clients/credits");
        Roles([UserRoles.SuperAdmin, UserRoles.Administrator]);
        Options(x => x.WithTags("Clients"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var clients = await _context.Clients.ToListAsync(cancellationToken: cancellationToken);

        if (clients.Count is 0)
        {
            await SendAsync([], cancellation: cancellationToken);
            return;
        }

        await SendAsync(clients
            .Select(x => new GetClientCreditsResponse(x.Id, x.Credits, x.FirstName, x.LastName))
            .ToList(), cancellation: cancellationToken);
    }
}