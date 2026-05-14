using ApexPerformance.API.Database;
using ApexPerformance.API.Database.Entities;
using ApexPerformance.API.Services.Interfaces;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Fcm;

public record RegisterDeviceTokenRequest(string Token, string Platform);

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
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedBy = userId,
                UpdatedAt = DateTimeOffset.UtcNow,
                UpdatedBy = userId
            });
        }
        else
        {
            existing.UserId = userId;
            existing.UpdatedAt = DateTimeOffset.UtcNow;
            existing.UpdatedBy = userId;
        }

        await _db.SaveChangesAsync(cancellationToken);
        
        await SendOkAsync(cancellationToken);
    }
}