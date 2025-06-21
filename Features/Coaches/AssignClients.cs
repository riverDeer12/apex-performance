using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Database.Entities;
using FastEndpoints;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Coaches;

public record AssignClientsRequest(
    List<Guid> Clients
);

public record AssignClientsResponse(
    Guid Id,
    string FirstName,
    string LastName
);

public class AssignClientsEndpoint : Endpoint<AssignClientsRequest, List<AssignClientsResponse>>
{
    private readonly ApexPerformanceContext _context;

    public AssignClientsEndpoint(ApexPerformanceContext context)
    {
        _context = context;
    }

    public override void Configure()
    {
        Post("api/coaches/{id}/assign-clients");
        Roles([UserRoles.SuperAdmin, UserRoles.Administrator]);
        Options(x => x.WithTags("Coaches"));
    }

    public override async Task HandleAsync(AssignClientsRequest request, CancellationToken cancellationToken)
    {
        var coachId = Route<Guid>("id", isRequired: true);

        var coach =
            await _context.Coaches
                .FirstOrDefaultAsync(x => x.Id == coachId, cancellationToken: cancellationToken);

        if (coach is null)
            ThrowError(ErrorMessages.NotFound);

        var clients = await _context.Clients
            .Where(x => request.Clients.Contains(x.Id))
            .ToListAsync(cancellationToken: cancellationToken);

        if (clients.Count == 0)
            ThrowError(ErrorMessages.NotFound);

        var coachClients = clients
            .Select(client => new CoachClient
            {
                ClientId = client.Id,
                Client = client,
                CoachId = coach.Id,
                Coach = coach
            }).ToList();

        _context.CoachClients.AddRange(coachClients);

        var relationsResult = await _context.SaveChangesAsync(cancellationToken);

        if (relationsResult == 0)
            ThrowError(ErrorMessages.SavingError);

        await SendAsync(coachClients.Select(x
                => new AssignClientsResponse(x.ClientId, x.Client.FirstName, x.Client.LastName)).ToList(),
            cancellation: cancellationToken);
    }
}

public sealed class AssignClientsValidator : Validator<AssignClientsRequest>
{
    public AssignClientsValidator()
    {
        RuleFor(x => x.Clients).NotEmpty().WithMessage(ValidationMessages.Required);
    }
}