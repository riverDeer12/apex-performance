using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Database.Entities;
using ApexPerformance.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Trainings;

/// <summary>
/// Administrators see and manage all trainings, coaches only
/// trainings of their clients and clients only their own completed ones.
/// </summary>
public static class TrainingAccess
{
    public static bool IsAdministrator(ICurrentUserService currentUserService)
        => currentUserService.LoggedUserHasRole(UserRoles.SuperAdmin) ||
           currentUserService.LoggedUserHasRole(UserRoles.Administrator);

    public static async Task<IQueryable<Training>> GetVisibleTrainings(ApexPerformanceContext context,
        ICurrentUserService currentUserService, CancellationToken cancellationToken)
    {
        if (IsAdministrator(currentUserService))
            return context.Trainings;

        if (currentUserService.LoggedUserHasRole(UserRoles.Coach))
        {
            var clientIds = await GetCoachClientIds(context, currentUserService, cancellationToken);

            return context.Trainings.Where(x => clientIds.Contains(x.ClientId));
        }

        // Clients only see trainings their coach marked as completed,
        // planned trainings are for the coach.
        if (currentUserService.LoggedUserHasRole(UserRoles.Client))
            return context.Trainings.Where(x => x.Client.UserId == currentUserService.UserId && x.IsCompleted);

        return context.Trainings.Where(_ => false);
    }

    public static async Task<bool> CanManageClient(ApexPerformanceContext context,
        ICurrentUserService currentUserService, Guid clientId, CancellationToken cancellationToken)
    {
        if (IsAdministrator(currentUserService))
            return true;

        if (!currentUserService.LoggedUserHasRole(UserRoles.Coach))
            return false;

        var clientIds = await GetCoachClientIds(context, currentUserService, cancellationToken);

        return clientIds.Contains(clientId);
    }

    private static async Task<List<Guid>> GetCoachClientIds(ApexPerformanceContext context,
        ICurrentUserService currentUserService, CancellationToken cancellationToken)
        => await context.CoachClients
            .Where(x => x.Coach.UserId == currentUserService.UserId)
            .Select(x => x.ClientId)
            .ToListAsync(cancellationToken);
}
