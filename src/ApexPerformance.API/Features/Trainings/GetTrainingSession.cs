using ApexPerformance.API.Database;
using ApexPerformance.API.Features.Profile;
using ApexPerformance.API.Services.Interfaces;
using ApexPerformance.API.Utilities;
using ApexPerformance.API.Constants;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Trainings;

/// <summary>
/// The trainings of the session the training belongs to (clients who train at
/// the same time with the coach), the training itself included, oldest first,
/// so the apps edit them together. Only those the caller may see. Trainings
/// with a session id are grouped by it; older ones (saved before the id) by
/// the same creator and the same start to the second. A training on its own
/// returns only itself.
/// </summary>
public class GetTrainingSessionEndpoint : EndpointWithoutRequest<List<TrainingResponse>>
{
    private readonly ApexPerformanceContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetTrainingSessionEndpoint(ApexPerformanceContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public override void Configure()
    {
        Get("api/trainings/{id}/session");
        Options(x => x.WithTags("Trainings"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var trainingId = Route<Guid>("id", isRequired: true);
        var visible = await TrainingAccess.GetVisibleTrainings(_context, _currentUserService, cancellationToken);

        var training = await visible.AsNoTracking()
            .Where(x => x.Id == trainingId)
            .Select(x => new { x.Id, x.SessionId, x.StartedAt, x.CreatedBy })
            .FirstOrDefaultAsync(cancellationToken);

        if (training is null)
            ThrowError(ErrorCodes.NotFound);

        var query = visible.AsNoTracking();

        if (training.SessionId is { } sessionId)
        {
            query = query.Where(x => x.SessionId == sessionId);
        }
        else if (training.StartedAt is { } start)
        {
            var from = start.AddSeconds(-1);
            var to = start.AddSeconds(1);
            query = query.Where(x => x.Id == trainingId ||
                                     x.SessionId == null && x.CreatedBy == training.CreatedBy &&
                                     x.StartedAt >= from && x.StartedAt <= to);
        }
        else
        {
            query = query.Where(x => x.Id == trainingId);
        }

        var trainings = (await query
                .Include(x => x.Client)
                .Include(x => x.Exercises)
                .ThenInclude(x => x.Workout)
                .Include(x => x.Exercises)
                .ThenInclude(x => x.Sets)
                .AsSplitQuery()
                .ToListAsync(cancellationToken))
            .Where(x => TrainingSessions.SameSession(
                new TrainingSessions.Key(training.Id, training.SessionId, training.CreatedBy, training.StartedAt),
                new TrainingSessions.Key(x.Id, x.SessionId, x.CreatedBy, x.StartedAt)))
            .OrderBy(x => x.CreatedAt)
            .ThenBy(x => x.Id)
            .ToList();

        var pictures = await UserPictures.LoadAsync(_context, trainings.Select(x => x.Client.UserId),
            cancellationToken);

        await SendAsync(trainings.Select(x => TrainingMapper.ToResponse(x, pictures)).ToList(),
            cancellation: cancellationToken);
    }
}
