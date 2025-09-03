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
        Roles(UserRoles.SuperAdmin, UserRoles.Administrator, UserRoles.Coach);
        Options(x => x.WithTags("FunctionalMovementScreens"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var functionalMovementScreens =
            _currentUserService.LoggedUserHasRole(UserRoles.Coach)
                ? await GetCoachFunctionalMovementScreens()
                : await GetAllFunctionalMovementScreens();

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
                new PersonDataDto(x.Client.Id, x.Client.FirstName, x.Client.LastName)
            )
        ).ToList(), cancellation: cancellationToken);
    }

    private async Task<List<FunctionalMovementScreen>> GetAllFunctionalMovementScreens()
    {
        return await _context.FunctionalMovementScreens
            .Where(x => !x.IsDeleted)
            .Include(functionalMovementScreen => functionalMovementScreen.Client)
            .ToListAsync();
    }

    private async Task<List<FunctionalMovementScreen>> GetCoachFunctionalMovementScreens()
    {
        var coach = await _context.Coaches.FirstOrDefaultAsync(x => x.UserId == _currentUserService.UserId);

        if (coach is null)
            ThrowError(ErrorMessages.NotFound);

        var coachClientsIds = await _context.CoachClients
            .Where(x => x.CoachId == coach.Id)
            .Select(x => x.ClientId)
            .ToListAsync();

        if (coachClientsIds.Count is 0) return [];

        return await _context.FunctionalMovementScreens
            .Where(x => coachClientsIds.Contains(x.ClientId) && !x.IsDeleted)
            .Include(functionalMovementScreen => functionalMovementScreen.Client)
            .ToListAsync();
    }
}