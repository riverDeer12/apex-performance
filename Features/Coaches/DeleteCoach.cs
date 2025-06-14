using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Coaches;

public record DeleteCoachResponse(Guid Id, string FirstName, string LastName);

public class DeleteCoachEndpoint : EndpointWithoutRequest<DeleteCoachResponse>
{
    private readonly ApexPerformanceContext _context;

    public DeleteCoachEndpoint(ApexPerformanceContext context)
    {
        _context = context;
    }

    public override void Configure()
    {
        Delete("api/coaches/{id}");
        Roles([UserRoles.SuperAdmin, UserRoles.Administrator]);
        Options(x => x.WithTags("Coaches"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var coaches = Route<Guid>("id", isRequired: true);

        var coach =
            await _context.Coaches
                .FirstOrDefaultAsync(x => x.Id == coaches, cancellationToken: cancellationToken);

        if (coach is null)
            ThrowError(ErrorMessages.NotFound);

        coach.Delete();

        _context.Coaches.Update(coach);

        var result = await _context.SaveChangesAsync(cancellationToken);

        if (result == 0)
            ThrowError(ErrorMessages.SavingError);

        await SendAsync(new DeleteCoachResponse(coach.Id, coach.FirstName, coach.LastName),
            cancellation: cancellationToken);
    }
}