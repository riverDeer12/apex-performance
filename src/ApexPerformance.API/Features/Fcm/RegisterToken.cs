using ApexPerformance.API.Database;
using ApexPerformance.API.Database.Entities;
using ApexPerformance.API.Services.Interfaces;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Fcm;

// Device info is optional so app versions that only send token and platform keep working.
public record RegisterDeviceTokenRequest(
    string Token,
    string Platform,
    string? AppVersion = null,
    string? BuildNumber = null,
    string? OsVersion = null,
    string? DeviceModel = null);

public class RegisterTokenEndpoint : Endpoint<RegisterDeviceTokenRequest>
{
    private readonly ApexPerformanceContext _db;
    private readonly ICurrentUserService _currentUserService;

    public override void Configure()
    {
        Post("api/fcm-tokens");
    }

    public RegisterTokenEndpoint(ICurrentUserService currentUserService, ApexPerformanceContext db)
    {
        _currentUserService = currentUserService;
        _db = db;
    }

    public override async Task HandleAsync(RegisterDeviceTokenRequest request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;

        var existing = await _db.DeviceTokens
            .FirstOrDefaultAsync(t => t.Token == request.Token, cancellationToken);

        if (existing is null)
        {
            _db.DeviceTokens.Add(new DeviceToken
            {
                UserId = userId,
                Token = request.Token,
                Platform = request.Platform,
                AppVersion = request.AppVersion,
                BuildNumber = request.BuildNumber,
                OsVersion = request.OsVersion,
                DeviceModel = request.DeviceModel,
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedBy = userId,
                UpdatedAt = DateTimeOffset.UtcNow,
                UpdatedBy = userId
            });
        }
        else
        {
            existing.UserId = userId;
            existing.Platform = request.Platform;
            existing.AppVersion = request.AppVersion;
            existing.BuildNumber = request.BuildNumber;
            existing.OsVersion = request.OsVersion;
            existing.DeviceModel = request.DeviceModel;
            existing.UpdatedAt = DateTimeOffset.UtcNow;
            existing.UpdatedBy = userId;
        }

        await _db.SaveChangesAsync(cancellationToken);
        
        await SendOkAsync(cancellationToken);
    }
}