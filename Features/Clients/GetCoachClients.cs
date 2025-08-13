using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Services;
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
    private readonly ICurrentUserService _currentUserService;

    public GetCoachClientsEndpoint(ApexPerformanceContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public override void Configure()
    {
        Get("api/clients/coach");
        Permissions([nameof(UserPermissions.CanGetClients)]);
        Options(x => x.WithTags("Clients"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var coach = await _context.Coaches.FirstOrDefaultAsync(x => x.UserId == _currentUserService.UserId,
            cancellationToken: cancellationToken);
        
        if(coach is null)
            ThrowError(ErrorMessages.NotFound);

        var clientIds = _context.CoachClients
            .Where(x => x.CoachId == coach.Id)
            .Select(x => x.ClientId)
            .ToList();

        if (clientIds.Count == 0)
        {
            await SendAsync([], cancellation: cancellationToken);
            return;
        }

        var clients = await _context.Clients
            .Where(x => clientIds.Contains(x.Id) && !x.IsDeleted)
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