using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Shared.DataTransferObjects;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Clients;

public class GetClientsByCoachIdEndpoint : EndpointWithoutRequest<List<PersonDataDto>>
{
    private readonly ApexPerformanceContext _context;

    public GetClientsByCoachIdEndpoint(ApexPerformanceContext context)
    {
        _context = context;
    }

    public override void Configure()
    {
        Get("api/clients/coach/{id}");
        Roles(UserRoles.SuperAdmin, UserRoles.Administrator, UserRoles.Coach);
        Options(x => x.WithTags("Clients"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var coachId = Route<Guid>("id", isRequired: true);

        var coach = await _context.Coaches.FirstOrDefaultAsync(x => x.Id == coachId,
            cancellationToken: cancellationToken);

        if (coach is null)
            ThrowError(ErrorMessages.NotFound);

        var coachClients = await _context.CoachClients
            .Where(x => x.CoachId == coachId)
            .Select(x => x.Client)
            .ToListAsync(cancellationToken: cancellationToken);

        await SendAsync(coachClients.Select(x => new PersonDataDto(x.Id, x.FirstName, x.LastName)).ToList(),
            cancellation: cancellationToken);
    }
}