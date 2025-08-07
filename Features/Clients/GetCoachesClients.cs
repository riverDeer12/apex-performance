using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Shared.DataTransferObjects;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Clients;

public record GetCoachesClientsRequest(List<Guid> Coaches);

public class GetCoachesClientsEndpoint : Endpoint<GetCoachesClientsRequest, List<PersonDataDto>>
{
    private readonly ApexPerformanceContext _context;

    public GetCoachesClientsEndpoint(ApexPerformanceContext context)
    {
        _context = context;
    }

    public override void Configure()
    {
        Post("api/clients/coaches");
        Permissions([nameof(UserPermissions.CanGetClients)]);
        Options(x => x.WithTags("Clients"));
    }

    public override async Task HandleAsync(GetCoachesClientsRequest request, CancellationToken cancellationToken)
    {
        var coaches = _context.Coaches
            .Where(x => request.Coaches.Contains(x.Id))
            .Select(x => x.Id)
            .ToList();

        if (coaches.Count == 0)
        {
            await SendAsync([], cancellation: cancellationToken);
            return;
        }

        var relatedClients = await _context.CoachClients
            .Where(x => coaches.Contains(x.CoachId))
            .Select(x => x.Client)
            .ToListAsync(cancellationToken: cancellationToken);

        if (relatedClients.Count == 0)
        {
            await SendAsync([], cancellation: cancellationToken);
            return;
        }

        await SendAsync(relatedClients.Select(x => new PersonDataDto(x.Id, x.FirstName, x.LastName))
                .ToList(),
            cancellation: cancellationToken);
    }
}