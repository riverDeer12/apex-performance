using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Services.Interfaces;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Profile;

public record ProfileResponse(
    Guid UserId,
    string Username,
    string Email,
    List<string> Roles,
    // Client, Coach, Administrator or null when user
    // has no personal data (e.g. only super admin).
    string? ProfileType,
    string? FirstName,
    string? LastName,
    string? Phone,
    bool HasProfilePicture,
    DateTimeOffset? ProfilePictureUpdatedAt
);

/// <summary>
/// Profile of logged user.
/// </summary>
public class GetProfileEndpoint : EndpointWithoutRequest<ProfileResponse>
{
    private readonly ApexPerformanceContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetProfileEndpoint(ApexPerformanceContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public override void Configure()
    {
        Get("api/profile");
        Options(x => x.WithTags("Profile"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var profile = await ProfileQueries.GetProfile(_context, _currentUserService.UserId, cancellationToken);

        if (profile is null)
            ThrowError(ErrorCodes.UserNotFound);

        await SendAsync(profile, cancellation: cancellationToken);
    }
}

public static class ProfileQueries
{
    public static async Task<ProfileResponse?> GetProfile(ApexPerformanceContext context, Guid userId,
        CancellationToken cancellationToken)
    {
        var user = await context.Users
            .AsNoTracking()
            .Where(x => x.Id == userId)
            .Select(x => new
            {
                x.Id,
                x.UserName,
                x.Email,
                Roles = x.Roles.Select(userRole => userRole.Role.Name).ToList(),
                Client = x.Client == null
                    ? null
                    : new { x.Client.FirstName, x.Client.LastName, x.Client.Phone },
                Coach = x.Coach == null
                    ? null
                    : new { x.Coach.FirstName, x.Coach.LastName, x.Coach.Phone },
                Administrator = x.Administrator == null
                    ? null
                    : new { x.Administrator.FirstName, x.Administrator.LastName }
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (user is null) return null;

        var pictureUpdatedAt = await context.UserProfilePictures
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .Select(x => (DateTimeOffset?)x.UpdatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        var (type, firstName, lastName, phone) =
            user.Client is not null ? ("Client", user.Client.FirstName, user.Client.LastName, user.Client.Phone) :
            user.Coach is not null ? ("Coach", user.Coach.FirstName, user.Coach.LastName, user.Coach.Phone) :
            user.Administrator is not null
                ? ("Administrator", user.Administrator.FirstName, user.Administrator.LastName, (string?)null)
                : ((string?)null, (string?)null, (string?)null, (string?)null);

        return new ProfileResponse(user.Id, user.UserName, user.Email, user.Roles, type, firstName, lastName,
            phone, pictureUpdatedAt is not null, pictureUpdatedAt);
    }
}
