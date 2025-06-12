using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Clients;

public record GetCoachClientsEndpointResponse(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string Phone,
    int Credits,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    bool IsDeleted,
    ClientUserDto User);

public sealed record ClientUserDto(Guid Id, string Username, string Email);

public class GetCoachClientsEndpoint : EndpointWithoutRequest<List<GetCoachClientsEndpointResponse>>
{
    private readonly ApexPerformanceContext _context;

    public GetCoachClientsEndpoint(ApexPerformanceContext context)
    {
        _context = context;
    }

    public override void Configure()
    {
        Get("api/clients/coach/{id}");
        Permissions([nameof(UserPermissions.CanGetClients)]);
        Options(x => x.WithTags("Clients"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var clients = await _context.Clients
            .Where(x => !x.IsDeleted)
            .Include(userType => userType.User)
            .ToListAsync(cancellationToken: cancellationToken);

        if (clients.Count is 0)
        {
            await SendAsync([], cancellation: cancellationToken);
            return;
        }

        var response = new List<GetCoachClientsEndpointResponse>();

        foreach (var client in clients)
        {
            var clientUser = client.User;

            if (clientUser == null) continue;

            var clientUserResponse = new ClientUserDto(clientUser.Id, clientUser.UserName, clientUser.Email);

            var roleResponse = new GetCoachClientsEndpointResponse(client.Id,
                client.FirstName, client.LastName, client.Email,
                client.Phone,
                client.Credits,
                client.CreatedAt,
                client.UpdatedAt,
                client.IsDeleted, clientUserResponse);

            response.Add(roleResponse);
        }

        await SendAsync(response, cancellation: cancellationToken);
    }
}