using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Features.Coaches;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Clients;

public record GetAllClientResponse(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string Phone,
    int Credits,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    bool IsDeleted,
    List<ClientCoachDto> Coaches,
    ClientUserDto User);

public record ClientCoachDto(
    Guid Id,
    string FirstName,
    string LastName
);

public class GetAllClientsEndpoint : EndpointWithoutRequest<List<GetAllClientResponse>>
{
    private readonly ApexPerformanceContext _context;

    public GetAllClientsEndpoint(ApexPerformanceContext context)
    {
        _context = context;
    }

    public override void Configure()
    {
        Get("api/clients/all");
        Roles([UserRoles.SuperAdmin, UserRoles.Administrator]);
        Options(x => x.WithTags("Clients"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var clients = await _context.Clients
            .Include(userType => userType.User)
            .Include(client => client.Coaches)
            .ThenInclude(coachClient => coachClient.Coach)
            .ToListAsync(cancellationToken: cancellationToken);

        if (clients.Count is 0)
        {
            await SendAsync([], cancellation: cancellationToken);
            return;
        }

        var response = new List<GetAllClientResponse>();

        foreach (var client in clients)
        {
            var clientUser = client.User;

            if (clientUser == null) continue;

            var clientCoaches = new List<ClientCoachDto>();

            if (client.Coaches.Count != 0)
            {
                clientCoaches.AddRange(client.Coaches.Select(clientCoach =>
                    new ClientCoachDto(clientCoach.Coach.Id, clientCoach.Coach.FirstName,
                        clientCoach.Coach.LastName)));
            }

            var clientUserResponse = new ClientUserDto(clientUser.Id, clientUser.UserName, clientUser.Email);

            var roleResponse = new GetAllClientResponse(client.Id,
                client.FirstName, client.LastName, client.Email,
                client.Phone,
                client.Credits,
                client.CreatedAt,
                client.UpdatedAt,
                client.IsDeleted,
                clientCoaches,
                clientUserResponse);

            response.Add(roleResponse);
        }

        await SendAsync(response, cancellation: cancellationToken);
    }
}