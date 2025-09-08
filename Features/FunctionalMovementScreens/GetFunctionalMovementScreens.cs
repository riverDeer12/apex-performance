using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Database.Entities;
using ApexPerformance.API.Services;
using ApexPerformance.API.Shared.DataTransferObjects;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.FunctionalMovementScreens;

public record FunctionalMovementScreenResponse(
    Guid Id,
    string DeepSquat,
    string HurdleStep,
    string InLineLunge,
    string ActiveStraightLegRaise,
    string TrunkStabilityPushUp,
    string RotaryStability,
    string ShoulderMobility,
    DateTimeOffset CreatedAt,
    PersonDataDto Client
);

public class GetFunctionalMovementScreensEndpoint : EndpointWithoutRequest<List<FunctionalMovementScreenResponse>>
{
    private readonly ApexPerformanceContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetFunctionalMovementScreensEndpoint(ApexPerformanceContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public override void Configure()
    {
        Get("api/functional-movement-screens");
        Options(x => x.WithTags("FunctionalMovementScreens"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var functionalMovementScreens =
            await GetFunctionalMovementScreensForUser(cancellationToken);

        if (functionalMovementScreens.Count is 0)
        {
            await SendAsync([], cancellation: cancellationToken);
            return;
        }

        await SendAsync(functionalMovementScreens.Select(x =>
            new FunctionalMovementScreenResponse(
                x.Id,
                x.DeepSquat,
                x.HurdleStep,
                x.InLineLunge,
                x.ActiveStraightLegRaise,
                x.TrunkStabilityPushUp,
                x.RotaryStability,
                x.ShoulderMobility,
                x.CreatedAt,
                new PersonDataDto(x.Client.Id, x.Client.FirstName, x.Client.LastName)
            )
        ).ToList(), cancellation: cancellationToken);
    }

    private async Task<List<FunctionalMovementScreen>> GetFunctionalMovementScreensForUser(
        CancellationToken cancellationToken)
    {
        if (_currentUserService.LoggedUserHasRole(UserRoles.SuperAdmin) ||
            _currentUserService.LoggedUserHasRole(UserRoles.Administrator))
            return await GetAllFunctionalMovementScreens(cancellationToken);

        if (_currentUserService.LoggedUserHasRole(UserRoles.Coach))
            return await GetCoachFunctionalMovementScreens(cancellationToken);

        if (_currentUserService.LoggedUserHasRole(UserRoles.Client))
            return await GetClientFunctionalMovementScreens(cancellationToken);

        return new List<FunctionalMovementScreen>();
    }

    private async Task<List<FunctionalMovementScreen>> GetAllFunctionalMovementScreens(
        CancellationToken cancellationToken)
    {
        return await _context.FunctionalMovementScreens
            .Include(functionalMovementScreen => functionalMovementScreen.Client)
            .ToListAsync(cancellationToken: cancellationToken);
    }

    private async Task<List<FunctionalMovementScreen>> GetCoachFunctionalMovementScreens(
        CancellationToken cancellationToken)
    {
        var coach = await _context.Coaches.FirstOrDefaultAsync(x => x.UserId == _currentUserService.UserId,
            cancellationToken: cancellationToken);

        if (coach is null)
            ThrowError(ErrorMessages.NotFound);

        var coachClientsIds = await _context.CoachClients
            .Where(x => x.CoachId == coach.Id)
            .Select(x => x.ClientId)
            .ToListAsync(cancellationToken: cancellationToken);

        if (coachClientsIds.Count is 0) return [];

        return await _context.FunctionalMovementScreens
            .Where(x => coachClientsIds.Contains(x.ClientId))
            .Include(functionalMovementScreen => functionalMovementScreen.Client)
            .ToListAsync(cancellationToken: cancellationToken);
    }

    private async Task<List<FunctionalMovementScreen>> GetClientFunctionalMovementScreens(
        CancellationToken cancellationToken)
    {
        var client = await _context.Clients.FirstOrDefaultAsync(x => x.UserId == _currentUserService.UserId,
            cancellationToken: cancellationToken);

        if (client is null)
            ThrowError(ErrorMessages.NotFound);

        return await _context.FunctionalMovementScreens
            .Where(x => x.ClientId == client.Id)
            .Include(functionalMovementScreen => functionalMovementScreen.Client)
            .ToListAsync(cancellationToken: cancellationToken);
    }
}