using ApexPerformance.API.Features.Profile;
using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Coaches;

public record GetCoachResponse(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string Phone,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    bool IsDeleted,
    string FullName,
    List<CoachClientDto> Clients,
    CoachUserDto User,
    // Profile picture link (see UserPictures), null when there is none.
    string? PictureUrl = null
);

public record CoachUserDto(Guid Id, string Username, string Email);

public record CoachClientDto(Guid Id, string FirstName, string LastName);

public class GetCoachesEndpoint : EndpointWithoutRequest<List<GetCoachResponse>>
{
    private readonly ApexPerformanceContext _context;

    public GetCoachesEndpoint(ApexPerformanceContext context)
    {
        _context = context;
    }

    public override void Configure()
    {
        Get("api/coaches/all");
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

        var pictures = await UserPictures.LoadAsync(_context, coaches.Select(x => x.UserId), cancellationToken);

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
                coach.FullName,
                coachClients,
                clientUserResponse,
                pictures.UrlFor(coach.UserId));

            response.Add(coachResponse);
        }

        await SendAsync(response,
            cancellation: cancellationToken);
    }
}