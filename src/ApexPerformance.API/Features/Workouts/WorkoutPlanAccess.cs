using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Database.Entities;
using ApexPerformance.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Workouts;

/// <summary>
/// Workouts a client can see depend on the plan agreed with the coach:
/// private coaching only mobility and stretching workouts, online
/// coaching all workouts and membership none. Staff see all workouts.
/// </summary>
public static class WorkoutPlanAccess
{
    // Parts of workout type names, in any language, for mobility and stretching.
    private static readonly string[] MobilityAndStretchingKeywords =
        ["mobil", "stretch", "istez", "fleksib", "flexib", "allung"];

    /// <summary>
    /// Filter for the logged user's workouts, or null when all workouts are visible.
    /// </summary>
    public static async Task<Func<Workout, bool>?> GetFilter(ApexPerformanceContext context,
        ICurrentUserService currentUserService, CancellationToken cancellationToken)
    {
        if (!currentUserService.LoggedUserHasRole(UserRoles.Client))
            return null;

        var plan = await context.Clients
            .Where(x => x.UserId == currentUserService.UserId)
            .Select(x => x.Plan)
            .FirstOrDefaultAsync(cancellationToken);

        if (plan == ClientPlans.OnlineCoaching)
            return null;

        if (plan == ClientPlans.PrivateCoaching)
            return IsMobilityOrStretching;

        // Membership, or no client data for the user.
        return _ => false;
    }

    /// <summary>
    /// True when one of the workout's types is mobility or stretching.
    /// Workout types need to be loaded.
    /// </summary>
    public static bool IsMobilityOrStretching(Workout workout)
        => workout.WorkoutTypes.Any(x => MobilityAndStretchingKeywords.Any(keyword =>
            x.WorkoutType.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase)));
}
