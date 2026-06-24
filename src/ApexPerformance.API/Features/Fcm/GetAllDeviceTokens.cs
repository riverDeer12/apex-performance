using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Fcm;

public record DeviceTokenResponse(
    Guid Id,
    string Username,
    string Token,
    string Platform,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public class GetAllDeviceTokensEndpoint : EndpointWithoutRequest<List<DeviceTokenResponse>>
{
    private readonly ApexPerformanceContext _db;

    public GetAllDeviceTokensEndpoint(ApexPerformanceContext db)
    {
        _db = db;
    }

    public override void Configure()
    {
        Get("api/fcm-tokens");
        Roles(UserRoles.SuperAdmin, UserRoles.Administrator);
        Options(x => x.WithTags("Fcm"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var tokens = await _db.DeviceTokens
            .Include(t => t.User)
            .Where(t => !t.IsDeleted)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);

        await SendAsync(tokens.Select(t =>
                new DeviceTokenResponse(t.Id, t.User!.UserName, t.Token, t.Platform, t.CreatedAt, t.UpdatedAt))
            .ToList(), cancellation: cancellationToken);
    }
}