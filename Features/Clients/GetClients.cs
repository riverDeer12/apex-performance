using ApexPerformance.API.Database;
using ApexPerformance.API.Shared.DataTransferObjects;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Clients;

public class GetClientsEndpoint : EndpointWithoutRequest<List<PersonDataDto>>
{
    private readonly ApexPerformanceContext _context;

    public GetClientsEndpoint(ApexPerformanceContext context)
    {
        _context = context;
    }

    public override void Configure()
    {
        Get("api/clients");
        Options(x => x.WithTags("Clients"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var clients = await _context.Clients
            .Where(x => !x.IsDeleted)
            .ToListAsync(cancellationToken: cancellationToken);

        if (clients.Count is 0)
        {
            await SendAsync([], cancellation: cancellationToken);
            return;
        }

        await SendAsync(clients.Select(x => new PersonDataDto(x.Id, x.FirstName, x.LastName)).ToList(),
            cancellation: cancellationToken);
    }
}