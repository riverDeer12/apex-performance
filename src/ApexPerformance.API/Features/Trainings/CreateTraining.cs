using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Database.Entities;
using ApexPerformance.API.Services.Interfaces;
using FastEndpoints;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Trainings;

public record CreateTrainingRequest(
    Guid Client,
    string Name,
    DateTimeOffset Date,
    string? Note,
    bool IsCompleted,
    List<TrainingExerciseRequest>? Exercises
);

public class CreateTrainingEndpoint : Endpoint<CreateTrainingRequest, TrainingResponse>
{
    private readonly ApexPerformanceContext _context;
    private readonly ICurrentUserService _currentUserService;

    public CreateTrainingEndpoint(ApexPerformanceContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public override void Configure()
    {
        Post("api/trainings");
        // Clients can only view their trainings.
        Roles(UserRoles.SuperAdmin, UserRoles.Administrator, UserRoles.Coach);
        Options(x => x.WithTags("Trainings"));
    }

    public override async Task HandleAsync(CreateTrainingRequest request, CancellationToken cancellationToken)
    {
        var client = await _context.Clients.FirstOrDefaultAsync(x => x.Id == request.Client, cancellationToken);

        if (client is null)
            ThrowError(ErrorCodes.NotFound);

        if (!await TrainingAccess.CanManageClient(_context, _currentUserService, client.Id, cancellationToken))
            ThrowError(ErrorCodes.NotFound);

        if (!await TrainingValidation.WorkoutsExist(_context, request.Exercises, cancellationToken))
            ThrowError(ErrorCodes.NotValid);

        var training = new Training
        {
            Name = request.Name.Trim(),
            Date = request.Date,
            Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim(),
            IsCompleted = request.IsCompleted,
            CompletedAt = request.IsCompleted ? DateTimeOffset.UtcNow : null,
            Client = client,
            Exercises = TrainingMapper.ToExercises(request.Exercises)
        };

        _context.Trainings.Add(training);

        var result = await _context.SaveChangesAsync(cancellationToken);

        if (result == 0)
            ThrowError(ErrorCodes.SavingError);

        await _context.Entry(training).Collection(x => x.Exercises).Query().Include(x => x.Workout)
            .LoadAsync(cancellationToken);

        await SendAsync(TrainingMapper.ToResponse(training), cancellation: cancellationToken);
    }
}

public sealed class CreateTrainingValidator : Validator<CreateTrainingRequest>
{
    public CreateTrainingValidator()
    {
        RuleFor(x => x.Client).NotEmpty().WithMessage(ErrorCodes.Required);
        RuleFor(x => x.Name).NotEmpty().WithMessage(ErrorCodes.Required)
            .MaximumLength(200).WithMessage(ErrorCodes.NotValid);
        RuleFor(x => x.Date).NotEmpty().WithMessage(ErrorCodes.Required);
        RuleFor(x => x.Note).MaximumLength(2000).WithMessage(ErrorCodes.NotValid);
        RuleForEach(x => x.Exercises).SetValidator(new TrainingExerciseRequestValidator());
    }
}
