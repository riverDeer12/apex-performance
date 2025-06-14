using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Coaches;

public record GetCoachResponse(
    Guid Id,
    string FirstName,
    string Lastname,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt
);

public class GetCoachesEndpoint : EndpointWithoutRequest<List<GetCoachResponse>>
{
    private readonly ApexPerformanceContext _context;

    public GetCoachesEndpoint(ApexPerformanceContext context)
    {
        _context = context;
    }

    public override void Configure()
    {
        Get("api/coaches");
        Roles([UserRoles.SuperAdmin, UserRoles.Administrator]);
        Options(x => x.WithTags("Coaches"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var coaches = await _context.Coaches.ToListAsync(cancellationToken: cancellationToken);

        if (coaches.Count == 0)
        {
            await SendAsync([], cancellation: cancellationToken);
            return;
        }

        await SendAsync(coaches
            .Select(x => new GetCoachResponse(x.Id, 
                x.FirstName, x.LastName, x.CreatedAt, x.UpdatedAt)).ToList(), cancellation: cancellationToken);
    }
}