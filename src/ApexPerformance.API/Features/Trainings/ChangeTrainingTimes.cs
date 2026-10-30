using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Services.Interfaces;
using FastEndpoints;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Trainings;

// Null clears the time, e.g. both null when the training was started by mistake.
public record ChangeTrainingTimesRequest(DateTimeOffset? StartedAt, DateTimeOffset? EndedAt);

public record ChangeTrainingTimesResponse(Guid Id, DateTimeOffset? StartedAt, DateTimeOffset? EndedAt,
    TrainingHeartRateResponse? HeartRate);

/// <summary>
/// When the training really started and ended, marked by the coach in the
/// apps ("Start" and "End"). The client's phone then reads the heart rate
/// in between from Apple Health. Changed times clear the heart rate.
/// </summary>
public class ChangeTrainingTimesEndpoint : Endpoint<ChangeTrainingTimesRequest, ChangeTrainingTimesResponse>
{
    private readonly ApexPerformanceContext _context;
    private readonly ICurrentUserService _currentUserService;

    public ChangeTrainingTimesEndpoint(ApexPerformanceContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
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

        if (training.StartedAt != request.StartedAt || training.EndedAt != request.EndedAt)
        {
            training.StartedAt = request.StartedAt;
            training.EndedAt = request.EndedAt;
            training.HeartRateMin = null;
            training.HeartRateMax = null;
            training.HeartRateAverage = null;

            var result = await _context.SaveChangesAsync(cancellationToken);

            if (result == 0)
                ThrowError(ErrorCodes.SavingError);
        }

        await SendAsync(new ChangeTrainingTimesResponse(training.Id, training.StartedAt, training.EndedAt,
            TrainingMapper.HeartRateOf(training)), cancellation: cancellationToken);
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
