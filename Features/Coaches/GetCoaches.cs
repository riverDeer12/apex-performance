using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Coaches;

public record GetCoachResponse(
    Guid Id,
    string Firstname,
    string Lastname,
    string Email,
    string Phone,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    bool IsDeleted,
    List<CoachClientDto> Clients,
    CoachUserDto User
);

public record CoachUserDto(Guid Id, string Username, string Email);

public record CoachClientDto(Guid Id, string Firstname, string Lastname);

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
        var coaches = await _context.Coaches
            .Include(userType => userType.User)
            .Include(coach => coach.Clients)
            .ThenInclude(coachClient => coachClient.Client)
            .ToListAsync(cancellationToken: cancellationToken);

        if (coaches.Count == 0)
        {
            await SendAsync([], cancellation: cancellationToken);
            return;
        }

        var response = new List<GetCoachResponse>();

        foreach (var coach in coaches)
        {
            var coachUser = coach.User;

            if (coachUser == null) continue;

            var clientUserResponse = new CoachUserDto(coachUser.Id, coachUser.UserName, coachUser.Email);

            var coachClients = new List<CoachClientDto>();

            if (coach.Clients.Count != 0)
            {
                coachClients.AddRange(coach.Clients.Select(coachClient =>
                    new CoachClientDto(coachClient.Client.Id, coachClient.Client.FirstName,
                        coachClient.Client.LastName)));
            }

            var coachResponse = new GetCoachResponse(coach.Id,
                coach.FirstName, coach.LastName, coach.Email,
                coach.Phone,
                coach.CreatedAt,
                coach.UpdatedAt,
                coach.IsDeleted,
                coachClients,
                clientUserResponse);

            response.Add(coachResponse);
        }

        await SendAsync(response,
            cancellation: cancellationToken);
    }
}