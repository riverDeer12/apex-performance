using ApexPerformance.API.Database;
using ApexPerformance.API.Services.Interfaces;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Trainings;

public class GetTrainingsEndpoint : EndpointWithoutRequest<List<TrainingResponse>>
{
    private readonly ApexPerformanceContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetTrainingsEndpoint(ApexPerformanceContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public override void Configure()
    {
        Get("api/trainings");
        Options(x => x.WithTags("Trainings"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var trainings = await (await TrainingAccess.GetVisibleTrainings(_context, _currentUserService,
                cancellationToken))
            .AsNoTracking()
            .Include(x => x.Client)
            .Include(x => x.Exercises)
            .ThenInclude(x => x.Workout)
            .Include(x => x.Exercises)
            .ThenInclude(x => x.Sets)
            .OrderByDescending(x => x.Date)
            .ToListAsync(cancellationToken);

        await SendAsync(trainings.Select(TrainingMapper.ToResponse).ToList(), cancellation: cancellationToken);
    }
}
