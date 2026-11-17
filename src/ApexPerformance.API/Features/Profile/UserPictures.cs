using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Services.Interfaces;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Profile;

/// <summary>
/// Profile picture links of the users shown in lists (clients, coaches,
/// chat, appointments...). A link is relative to the API base url
/// ("users/{userId}/picture?v=..."), loaded with the login token, and
/// changes when the picture changes, so apps can cache it by the link.
/// Null when the user has no picture: apps show initials or an icon.
/// </summary>
public sealed class UserPictures
{
    public static readonly UserPictures None = new(new Dictionary<Guid, DateTimeOffset>());

    private readonly IReadOnlyDictionary<Guid, DateTimeOffset> _updatedAt;

    public UserPictures(IReadOnlyDictionary<Guid, DateTimeOffset> updatedAt)
    {
        _updatedAt = updatedAt;
    }

    /// <summary>
    /// Link of the user's picture, null when the user has none.
    /// </summary>
    public string? UrlFor(Guid? userId) =>
        userId is { } id && _updatedAt.TryGetValue(id, out var updatedAt) ? Url(id, updatedAt) : null;

    public static string? Url(Guid userId, DateTimeOffset? updatedAt) =>
        updatedAt is null ? null : $"users/{userId}/picture?v={updatedAt.Value.ToUnixTimeMilliseconds()}";

    /// <summary>
    /// Loads when the pictures of these users changed, in one query
    /// that doesn't read the picture bytes.
    /// </summary>
    public static async Task<UserPictures> LoadAsync(ApexPerformanceContext context, IEnumerable<Guid> userIds,
        CancellationToken cancellationToken)
    {
        var ids = userIds.Where(x => x != Guid.Empty).Distinct().ToList();

        if (ids.Count == 0)
            return None;

        var updatedAt = await context.UserProfilePictures
            .AsNoTracking()
            .Where(x => ids.Contains(x.UserId))
            .Select(x => new { x.UserId, x.UpdatedAt })
            .ToDictionaryAsync(x => x.UserId, x => x.UpdatedAt, cancellationToken);

        return new UserPictures(updatedAt);
    }
}

/// <summary>
/// Profile picture of another user, as linked from the lists. Staff see
/// everyone's picture; a client sees their own, the staff's and those of
/// the clients they share an appointment with.
/// </summary>
public class GetUserPictureEndpoint : EndpointWithoutRequest
{
    private readonly ApexPerformanceContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetUserPictureEndpoint(ApexPerformanceContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public override void Configure()
    {
        Get("api/users/{userId}/picture");
        Options(x => x.WithTags("Profile"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var userId = Route<Guid>("userId", isRequired: true);

        if (!await CanSee(userId, cancellationToken))
        {
            await SendNotFoundAsync(cancellationToken);
            return;
        }

        var picture = await _context.UserProfilePictures
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);

        if (picture is null)
        {
            await SendNotFoundAsync(cancellationToken);
            return;
        }

        // Link carries the version (?v=), a new picture gets a new link.
        HttpContext.Response.Headers.CacheControl = "private, max-age=604800";

        await SendBytesAsync(picture.Content, contentType: picture.ContentType,
            lastModified: picture.UpdatedAt, cancellation: cancellationToken);
    }

    private async Task<bool> CanSee(Guid userId, CancellationToken cancellationToken)
    {
        var viewerId = _currentUserService.UserId;

        if (userId == viewerId)
            return true;

        var isStaff = _currentUserService.LoggedUserHasRole(UserRoles.SuperAdmin) ||
                      _currentUserService.LoggedUserHasRole(UserRoles.Administrator) ||
                      _currentUserService.LoggedUserHasRole(UserRoles.Coach);

        if (isStaff)
            return true;

        // Coaches and administrators are shown to clients everywhere.
        if (await _context.Coaches.AnyAsync(x => x.UserId == userId, cancellationToken) ||
            await _context.Administrators.AnyAsync(x => x.UserId == userId, cancellationToken))
            return true;

        // Another client: only from a shared (group) appointment.
        return await _context.ClientAppointments.AnyAsync(x =>
                x.Client.UserId == userId &&
                x.Appointment.Clients.Any(other => other.Client.UserId == viewerId),
            cancellationToken);
    }
}
