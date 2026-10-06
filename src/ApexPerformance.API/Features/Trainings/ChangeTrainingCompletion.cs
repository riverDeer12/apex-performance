using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Services.Interfaces;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Trainings;

public record ChangeTrainingCompletionRequest(bool IsCompleted);

public record ChangeTrainingCompletionResponse(Guid Id, bool IsCompleted, DateTimeOffset? CompletedAt);

/// <summary>
/// Mark training as completed or not completed,
/// only coaches and administrators can do it.
/// </summary>
public class ChangeTrainingCompletionEndpoint
    : Endpoint<ChangeTrainingCompletionRequest, ChangeTrainingCompletionResponse>
{
    private readonly ApexPerformanceContext _context;
    private readonly ICurrentUserService _currentUserService;

    public ChangeTrainingCompletionEndpoint(ApexPerformanceContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public override void Configure()
    {
        Put("api/trainings/{id}/completion");
        Roles(UserRoles.SuperAdmin, UserRoles.Administrator, UserRoles.Coach);
        Options(x => x.WithTags("Trainings"));
    }

    public override async Task HandleAsync(ChangeTrainingCompletionRequest request,
        CancellationToken cancellationToken)
    {
        var trainingId = Route<Guid>("id", isRequired: true);

        var training = await (await TrainingAccess.GetVisibleTrainings(_context, _currentUserService,
                cancellationToken))
            .FirstOrDefaultAsync(x => x.Id == trainingId, cancellationToken);

        if (training is null)
            ThrowError(ErrorCodes.NotFound);

        if (training.IsCompleted != request.IsCompleted)
        {
            training.IsCompleted = request.IsCompleted;
            training.CompletedAt = request.IsCompleted ? DateTimeOffset.UtcNow : null;

            var result = await _context.SaveChangesAsync(cancellationToken);

            if (result == 0)
                ThrowError(ErrorCodes.SavingError);
        }

        await SendAsync(new ChangeTrainingCompletionResponse(training.Id, training.IsCompleted,
            training.CompletedAt), cancellation: cancellationToken);
    }
}
