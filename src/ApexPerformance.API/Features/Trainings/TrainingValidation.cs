using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using FastEndpoints;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Trainings;

public sealed class TrainingExerciseSetRequestValidator : AbstractValidator<TrainingExerciseSetRequest>
{
    public TrainingExerciseSetRequestValidator()
    {
        RuleFor(x => x.Reps).MaximumLength(50).WithMessage(ErrorCodes.NotValid);
        RuleFor(x => x.Weight).InclusiveBetween(0, 9999).WithMessage(ErrorCodes.NotValid)
            .When(x => x.Weight is not null);
    }
}

public sealed class TrainingExerciseRequestValidator : AbstractValidator<TrainingExerciseRequest>
{
    public TrainingExerciseRequestValidator()
    {
        RuleFor(x => x.Workout).NotEmpty().WithMessage(ErrorCodes.Required);
        RuleFor(x => x.Note).MaximumLength(500).WithMessage(ErrorCodes.NotValid);
        RuleFor(x => x.Sets).Must(x => x is null || x.Count <= 50).WithMessage(ErrorCodes.NotValid);
        RuleForEach(x => x.Sets).SetValidator(new TrainingExerciseSetRequestValidator());
    }
}

public static class TrainingValidation
{
    /// <summary>
    /// Check that all exercises reference existing workouts.
    /// </summary>
    public static async Task<bool> WorkoutsExist(ApexPerformanceContext context,
        IEnumerable<TrainingExerciseRequest>? exercises, CancellationToken cancellationToken)
    {
        var workoutIds = (exercises ?? []).Select(x => x.Workout).Distinct().ToList();

        if (workoutIds.Count == 0)
            return true;

        var existingCount = await context.Workouts.CountAsync(x => workoutIds.Contains(x.Id), cancellationToken);

        return existingCount == workoutIds.Count;
    }
}
