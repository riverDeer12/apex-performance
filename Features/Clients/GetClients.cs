using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Database.Entities;
using ApexPerformance.API.Services;
using ApexPerformance.API.Shared.DataTransferObjects;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Clients;

public class GetClientsEndpoint : EndpointWithoutRequest<List<ClientDataDto>>
{
    private readonly ApexPerformanceContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetClientsEndpoint(ApexPerformanceContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public override void Configure()
    {
        Get("api/clients");
        Roles(UserRoles.SuperAdmin, UserRoles.Administrator, UserRoles.Coach);
        Options(x => x.WithTags("Clients"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var clients = await GetClientsForUser(cancellationToken);

        if (clients.Count is 0)
        {
            await SendAsync([], cancellation: cancellationToken);
            return;
        }

        await SendAsync(clients.Select(x =>
                    new ClientDataDto(x.Id, x.FirstName, x.LastName, x.Credits, x.Phone, x.Email, x.CreatedAt,
                        x.UpdatedAt))
                .ToList(),
            cancellation: cancellationToken);
    }

    private async Task<List<Client>> GetClientsForUser(CancellationToken cancellationToken)
    {
        if (_currentUserService.LoggedUserHasRole(UserRoles.SuperAdmin) ||
            _currentUserService.LoggedUserHasRole(UserRoles.Administrator))
            return await GetAllClients(cancellationToken);

        if (_currentUserService.LoggedUserHasRole(UserRoles.Coach))
            return await GetCoachClients(cancellationToken);

        return [];
    }

    private async Task<List<Client>> GetAllClients(CancellationToken cancellationToken)
    {
        return await _context.Clients
            .ToListAsync(cancellationToken: cancellationToken);
    }

    private async Task<List<Client>> GetCoachClients(CancellationToken cancellationToken)
    {
        var coach = await _context.Coaches.FirstOrDefaultAsync(x => x.UserId == _currentUserService.UserId,
            cancellationToken: cancellationToken);

        if (coach is null)
            ThrowError(ErrorMessages.NotFound);

        var coachClientsIds = await _context.CoachClients
            .Where(x => x.CoachId == coach.Id)
            .Select(x => x.ClientId)
            .ToListAsync(cancellationToken: cancellationToken);

        return await _context.Clients
            .Where(x => coachClientsIds.Contains(x.Id))
            .ToListAsync(cancellationToken: cancellationToken);
    }
}