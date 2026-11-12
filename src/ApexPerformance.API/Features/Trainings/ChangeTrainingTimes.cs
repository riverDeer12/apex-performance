using ApexPerformance.API.BackgroundJobs;
using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Services.Interfaces;
using ApexPerformance.API.Utilities;
using FastEndpoints;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Trainings;

// Null clears the time, e.g. both null when the training was started by mistake.
// DeviceToken: FCM token of the iPhone that changes the times. It shows the
// Live Activity itself, so it gets no push to start it.
// OnlyIfRunning: the times change only if the training still runs with this
// start (to the second), otherwise nothing changes and the current times are
// returned: "End training" of a reminder tapped after the training was ended
// or restarted elsewhere doesn't move its times.
public record ChangeTrainingTimesRequest(DateTimeOffset? StartedAt, DateTimeOffset? EndedAt,
    string? DeviceToken = null, bool OnlyIfRunning = false);

public record ChangeTrainingTimesResponse(Guid Id, DateTimeOffset? StartedAt, DateTimeOffset? EndedAt,
    TrainingHeartRateResponse? HeartRate, bool IsCompleted, DateTimeOffset? CompletedAt);

/// <summary>
/// When the training really started and ended, marked by the coach in the
/// apps ("Start" and "End"). The client's phone then reads the heart rate
/// in between from Apple Health. Changed times clear the heart rate.
/// Clients see a started training (in progress); ending it also marks it
/// as completed.
/// </summary>
public class ChangeTrainingTimesEndpoint : Endpoint<ChangeTrainingTimesRequest, ChangeTrainingTimesResponse>
{
    private readonly ApexPerformanceContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IPersonalRecordService _personalRecordService;

    public ChangeTrainingTimesEndpoint(ApexPerformanceContext context, ICurrentUserService currentUserService,
        IPersonalRecordService personalRecordService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _personalRecordService = personalRecordService;
    }

    public override void Configure()
    {
        Put("api/trainings/{id}/times");
        Roles(UserRoles.SuperAdmin, UserRoles.Administrator, UserRoles.Coach);
        Options(x => x.WithTags("Trainings"));
    }

    public override async Task HandleAsync(ChangeTrainingTimesRequest request, CancellationToken cancellationToken)
    {
        var trainingId = Route<Guid>("id", isRequired: true);

        var training = await (await TrainingAccess.GetVisibleTrainings(_context, _currentUserService,
                cancellationToken))
            .FirstOrDefaultAsync(x => x.Id == trainingId, cancellationToken);

        if (training is null)
            ThrowError(ErrorCodes.NotFound);

        var applies = !request.OnlyIfRunning ||
                      LiveActivityPushJob.IsRunning(training) && request.StartedAt is not null &&
                      Math.Abs((training.StartedAt!.Value - request.StartedAt.Value).TotalSeconds) < 1;

        if (applies && (training.StartedAt != request.StartedAt || training.EndedAt != request.EndedAt))
        {
            var wasRunning = LiveActivityPushJob.IsRunning(training);
            var previousStartedAt = training.StartedAt;

            training.StartedAt = request.StartedAt;
            training.EndedAt = request.EndedAt;
            training.HeartRateMin = null;
            training.HeartRateMax = null;
            training.HeartRateAverage = null;

            var completes = training.EndedAt is not null && !training.IsCompleted;
            if (completes)
            {
                training.IsCompleted = true;
                training.CompletedAt = training.EndedAt;
            }

            var result = await _context.SaveChangesAsync(cancellationToken);

            if (result == 0)
                ThrowError(ErrorCodes.SavingError);

            if (completes)
                await _personalRecordService.RecalculateForClientsAsync([training.ClientId], cancellationToken);

            // The training's Live Activity on the client's and the coach's iPhones.
            LiveActivityPushJob.Enqueue(training.Id, wasRunning, previousStartedAt, _currentUserService.UserId,
                string.IsNullOrWhiteSpace(request.DeviceToken) ? null : request.DeviceToken);

            // An hour after a start the coach is asked whether to end it (once per start).
            if (TrainingEndReminder.ShouldSchedule(previousStartedAt, training.StartedAt, training.EndedAt,
                    training.IsCompleted, DateTimeOffset.UtcNow))
                TrainingEndReminderJob.Schedule(training.Id, training.StartedAt!.Value, _currentUserService.UserId);
        }

        await SendAsync(new ChangeTrainingTimesResponse(training.Id, training.StartedAt, training.EndedAt,
            TrainingMapper.HeartRateOf(training), training.IsCompleted, training.CompletedAt),
            cancellation: cancellationToken);
    }
}

public sealed class ChangeTrainingTimesValidator : Validator<ChangeTrainingTimesRequest>
{
    // Longer is a forgotten "End".
    private static readonly TimeSpan MaxDuration = TimeSpan.FromHours(6);

    public ChangeTrainingTimesValidator()
    {
        RuleFor(x => x.EndedAt).Null().WithMessage(ErrorCodes.NotValid)
            .When(x => x.StartedAt is null);
        RuleFor(x => x).Must(x => x.EndedAt > x.StartedAt && x.EndedAt - x.StartedAt <= MaxDuration)
            .WithMessage(ErrorCodes.NotValid)
            .When(x => x.StartedAt is not null && x.EndedAt is not null);
    }
}
