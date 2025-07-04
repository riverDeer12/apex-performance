using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Services;
using ApexPerformance.API.Shared.DataTransferObjects;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Clients;

public class GetCoachRelatedClientsEndpoint : EndpointWithoutRequest<List<PersonDataDto>>
{
    private readonly ApexPerformanceContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetCoachRelatedClientsEndpoint(ApexPerformanceContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public override void Configure()
    {
        Get("api/clients/coach-related");
        Options(x => x.WithTags("Clients"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var client = await _context.Clients.FirstOrDefaultAsync(x => x.UserId == _currentUserService.UserId,
            cancellationToken: cancellationToken);

        if (client is null)
            ThrowError(ErrorMessages.NotFound);

        var coaches = _context.CoachClients
            .Where(x => x.ClientId == client.Id)
            .Include(coachClient => coachClient.Coach)
            .ThenInclude(coach => coach.Clients)
            .ThenInclude(coachClient => coachClient.Client)
            .Select(x => x.Coach)
            .ToList();

        var relatedClients = new List<PersonDataDto>();

        foreach (var coach in coaches)
        {
            var coachClients = coach.Clients
                .Select(x => new PersonDataDto(x.Client.Id, x.Client.FirstName, x.Client.LastName))
                .ToList();

            relatedClients.AddRange(coachClients);
        }

        var clients = relatedClients.DistinctBy(x => x.Id).ToList();

        await SendAsync(clients, cancellation: cancellationToken);
    }
}