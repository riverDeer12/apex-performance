using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Services.Interfaces;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Trainings;

public record DeleteTrainingResponse(Guid Id);

public class DeleteTrainingEndpoint : EndpointWithoutRequest<DeleteTrainingResponse>
{
    private readonly ApexPerformanceContext _context;
    private readonly ICurrentUserService _currentUserService;

    public DeleteTrainingEndpoint(ApexPerformanceContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public override void Configure()
    {
        Delete("api/trainings/{id}");
        Roles(UserRoles.SuperAdmin, UserRoles.Administrator, UserRoles.Coach);
        Options(x => x.WithTags("Trainings"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var trainingId = Route<Guid>("id", isRequired: true);

        var training = await (await TrainingAccess.GetVisibleTrainings(_context, _currentUserService,
                cancellationToken))
            .FirstOrDefaultAsync(x => x.Id == trainingId, cancellationToken);

        if (training is null)
            ThrowError(ErrorCodes.NotFound);

        training.Delete();

        var result = await _context.SaveChangesAsync(cancellationToken);

        if (result == 0)
            ThrowError(ErrorCodes.SavingError);

        await SendAsync(new DeleteTrainingResponse(training.Id), cancellation: cancellationToken);
    }
}
