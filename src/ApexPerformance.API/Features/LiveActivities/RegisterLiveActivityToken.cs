using ApexPerformance.API.BackgroundJobs;
using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Database.Entities;
using ApexPerformance.API.Features.Trainings;
using ApexPerformance.API.Services.Interfaces;
using FastEndpoints;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.LiveActivities;

/// <param name="DeviceToken">FCM registration token of the iPhone (POST api/fcm-tokens), of the logged user.</param>
/// <param name="Kind">"pushToStart" or "update".</param>
/// <param name="TrainingId">Training the activity shows, required for "update".</param>
/// <param name="Token">ActivityKit push token, hex.</param>
public record RegisterLiveActivityTokenRequest(string DeviceToken, string Kind, Guid? TrainingId, string Token);

/// <summary>
/// ActivityKit push tokens of the logged user's iPhone, so the backend can
/// start (push-to-start) and update or end (update token of one training)
/// the training's Live Activity. A newer token replaces the previous one.
/// See docs/LIVE_ACTIVITY_PUSH.md.
/// </summary>
public class RegisterLiveActivityTokenEndpoint : Endpoint<RegisterLiveActivityTokenRequest>
{
    private readonly ApexPerformanceContext _context;
    private readonly ICurrentUserService _currentUserService;

    public RegisterLiveActivityTokenEndpoint(ApexPerformanceContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public override void Configure()
    {
        Put("api/live-activity-tokens");
        Options(x => x.WithTags("Notifications"));
    }

    public override async Task HandleAsync(RegisterLiveActivityTokenRequest request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        var kind = LiveActivityTokens.ParseKind(request.Kind)!.Value;

        // Only the user's own devices.
        var device = await _context.DeviceTokens
            .FirstOrDefaultAsync(x => x.Token == request.DeviceToken && x.UserId == userId, cancellationToken);

        if (device is null)
            ThrowError(ErrorCodes.NotFound);

        Guid? trainingId = null;
        var trainingFinished = false;

        if (kind == LiveActivityTokenKind.Update)
        {
            // Only trainings the user sees: a client their trainings in progress.
            var training = await (await TrainingAccess.GetVisibleTrainings(_context, _currentUserService,
                    cancellationToken))
                .Where(x => x.Id == request.TrainingId)
                .Select(x => new { x.Id, x.StartedAt, x.EndedAt, x.IsCompleted })
                .FirstOrDefaultAsync(cancellationToken);

            if (training is null)
                ThrowError(ErrorCodes.NotFound);

            trainingId = training.Id;
            // Started and then ended or completed. Not a training that isn't started yet:
            // the app links its activity to the new training before saving its start.
            trainingFinished = training.StartedAt is not null && (training.EndedAt is not null || training.IsCompleted);
        }

        var existing = await _context.LiveActivityTokens
            .Where(x => x.DeviceTokenId == device.Id && x.Kind == kind && x.TrainingId == trainingId)
            .ToListAsync(cancellationToken);

        var token = existing.FirstOrDefault();
        if (token is null)
        {
            token = new LiveActivityToken { DeviceTokenId = device.Id, Kind = kind, TrainingId = trainingId, Token = request.Token };
            _context.LiveActivityTokens.Add(token);
        }

        token.Token = request.Token;
        token.UpdatedAt = DateTimeOffset.UtcNow;

        // Duplicates of a race between two requests.
        _context.LiveActivityTokens.RemoveRange(existing.Skip(1));

        await _context.SaveChangesAsync(cancellationToken);

        // The training ended before the activity registered (e.g. the iPhone
        // was asleep when it started): it ends now instead of running on.
        if (trainingId is not null && trainingFinished)
            LiveActivityPushJob.Enqueue(trainingId.Value, wasRunning: false);

        await SendOkAsync(cancellationToken);
    }
}

public sealed class RegisterLiveActivityTokenValidator : Validator<RegisterLiveActivityTokenRequest>
{
    public RegisterLiveActivityTokenValidator()
    {
        RuleFor(x => x.DeviceToken).NotEmpty().WithMessage(ErrorCodes.Required)
            .MaximumLength(200).WithMessage(ErrorCodes.NotValid);
        RuleFor(x => x.Token).NotEmpty().WithMessage(ErrorCodes.Required)
            .MaximumLength(400).WithMessage(ErrorCodes.NotValid);
        RuleFor(x => x.Kind).Must(x => LiveActivityTokens.ParseKind(x) is not null).WithMessage(ErrorCodes.NotValid);
        RuleFor(x => x.TrainingId).NotEmpty().WithMessage(ErrorCodes.Required)
            .When(x => LiveActivityTokens.ParseKind(x.Kind) == LiveActivityTokenKind.Update);
        RuleFor(x => x.TrainingId).Null().WithMessage(ErrorCodes.NotValid)
            .When(x => LiveActivityTokens.ParseKind(x.Kind) == LiveActivityTokenKind.PushToStart);
    }
}

public static class LiveActivityTokens
{
    public static LiveActivityTokenKind? ParseKind(string? kind)
        => Enum.TryParse<LiveActivityTokenKind>(kind, ignoreCase: true, out var parsed) &&
           Enum.IsDefined(parsed) && !int.TryParse(kind, out _)
            ? parsed
            : null;
}
