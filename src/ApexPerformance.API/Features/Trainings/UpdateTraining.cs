using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Services.Interfaces;
using FastEndpoints;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Trainings;

public record UpdateTrainingRequest(
    Guid Client,
    string Name,
    DateTimeOffset Date,
    string? Note,
    bool IsCompleted,
    List<TrainingExerciseRequest>? Exercises
);

public class UpdateTrainingEndpoint : Endpoint<UpdateTrainingRequest, TrainingResponse>
{
    private readonly ApexPerformanceContext _context;
    private readonly ICurrentUserService _currentUserService;

    public UpdateTrainingEndpoint(ApexPerformanceContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public override void Configure()
    {
        Put("api/trainings/{id}");
        // Clients can only view their trainings.
        Roles(UserRoles.SuperAdmin, UserRoles.Administrator, UserRoles.Coach);
        Options(x => x.WithTags("Trainings"));
    }

    public override async Task HandleAsync(UpdateTrainingRequest request, CancellationToken cancellationToken)
    {
        var trainingId = Route<Guid>("id", isRequired: true);

        var training = await (await TrainingAccess.GetVisibleTrainings(_context, _currentUserService,
                cancellationToken))
            .Include(x => x.Exercises)
            .FirstOrDefaultAsync(x => x.Id == trainingId, cancellationToken);

        if (training is null)
            ThrowError(ErrorCodes.NotFound);

        var client = await _context.Clients.FirstOrDefaultAsync(x => x.Id == request.Client, cancellationToken);

        if (client is null ||
            !await TrainingAccess.CanManageClient(_context, _currentUserService, client.Id, cancellationToken))
            ThrowError(ErrorCodes.NotFound);

        if (!await TrainingValidation.WorkoutsExist(_context, request.Exercises, cancellationToken))
            ThrowError(ErrorCodes.NotValid);

        training.Name = request.Name.Trim();
        training.Date = request.Date;
        training.Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim();
        training.Client = client;

        if (training.IsCompleted != request.IsCompleted)
        {
            training.IsCompleted = request.IsCompleted;
            training.CompletedAt = request.IsCompleted ? DateTimeOffset.UtcNow : null;
        }

        // Exercises are replaced, their order comes from the request.
        _context.TrainingExercises.RemoveRange(training.Exercises);
        training.Exercises = TrainingMapper.ToExercises(request.Exercises);

        var result = await _context.SaveChangesAsync(cancellationToken);

        if (result == 0)
            ThrowError(ErrorCodes.SavingError);

        await _context.Entry(training).Collection(x => x.Exercises).Query().Include(x => x.Workout)
            .LoadAsync(cancellationToken);

        await SendAsync(TrainingMapper.ToResponse(training), cancellation: cancellationToken);
    }
}

public sealed class UpdateTrainingValidator : Validator<UpdateTrainingRequest>
{
    public UpdateTrainingValidator()
    {
        RuleFor(x => x.Client).NotEmpty().WithMessage(ErrorCodes.Required);
        RuleFor(x => x.Name).NotEmpty().WithMessage(ErrorCodes.Required)
            .MaximumLength(200).WithMessage(ErrorCodes.NotValid);
        RuleFor(x => x.Date).NotEmpty().WithMessage(ErrorCodes.Required);
        RuleFor(x => x.Note).MaximumLength(2000).WithMessage(ErrorCodes.NotValid);
        RuleForEach(x => x.Exercises).SetValidator(new TrainingExerciseRequestValidator());
    }
}
