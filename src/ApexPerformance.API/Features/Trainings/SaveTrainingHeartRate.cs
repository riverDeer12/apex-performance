using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Services.Interfaces;
using FastEndpoints;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Trainings;

public record SaveTrainingHeartRateRequest(int Min, int Max, int Average);

/// <summary>
/// Client's heart rate during the training (between StartedAt and EndedAt),
/// read from Apple Health by the client's phone and sent once the training
/// is completed. Only the summary is kept, not the measurements.
/// </summary>
public class SaveTrainingHeartRateEndpoint : Endpoint<SaveTrainingHeartRateRequest, TrainingHeartRateResponse>
{
    private readonly ApexPerformanceContext _context;
    private readonly ICurrentUserService _currentUserService;

    public SaveTrainingHeartRateEndpoint(ApexPerformanceContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public override void Configure()
    {
        Put("api/trainings/{id}/heart-rate");
        Roles(UserRoles.Client);
        Options(x => x.WithTags("Trainings"));
    }

    public override async Task HandleAsync(SaveTrainingHeartRateRequest request, CancellationToken cancellationToken)
    {
        var trainingId = Route<Guid>("id", isRequired: true);

        // Clients see only their own completed trainings.
        var training = await (await TrainingAccess.GetVisibleTrainings(_context, _currentUserService,
                cancellationToken))
            .FirstOrDefaultAsync(x => x.Id == trainingId, cancellationToken);

        if (training is null)
            ThrowError(ErrorCodes.NotFound);

        // The heart rate belongs to the marked start and end.
        if (training.StartedAt is null || training.EndedAt is null)
            ThrowError(ErrorCodes.NotValid);

        training.HeartRateMin = request.Min;
        training.HeartRateMax = request.Max;
        training.HeartRateAverage = request.Average;

        await _context.SaveChangesAsync(cancellationToken);

        await SendAsync(new TrainingHeartRateResponse(request.Min, request.Max, request.Average),
            cancellation: cancellationToken);
    }
}

public sealed class SaveTrainingHeartRateValidator : Validator<SaveTrainingHeartRateRequest>
{
    public SaveTrainingHeartRateValidator()
    {
        RuleFor(x => x.Min).InclusiveBetween(25, 250).WithMessage(ErrorCodes.NotValid);
        RuleFor(x => x.Max).InclusiveBetween(25, 250).WithMessage(ErrorCodes.NotValid);
        RuleFor(x => x).Must(x => x.Min <= x.Average && x.Average <= x.Max).WithMessage(ErrorCodes.NotValid);
    }
}
