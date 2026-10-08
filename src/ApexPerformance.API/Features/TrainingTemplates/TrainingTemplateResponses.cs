using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Database.Entities;
using ApexPerformance.API.Features.Trainings;
using ApexPerformance.API.Services.Interfaces;
using FastEndpoints;
using FluentValidation;

namespace ApexPerformance.API.Features.TrainingTemplates;

public record TrainingTemplateResponse(
    Guid Id,
    string Name,
    string? Note,
    List<TrainingExerciseResponse> Exercises,
    string? AuthorName,
    // Coaches change only their own templates, administrators all of them.
    bool CanEdit,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt
);

public record TrainingTemplateRequest(
    string Name,
    string? Note,
    List<TrainingExerciseRequest>? Exercises
);

public sealed class TrainingTemplateRequestValidator : Validator<TrainingTemplateRequest>
{
    public TrainingTemplateRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage(ErrorCodes.Required)
            .MaximumLength(200).WithMessage(ErrorCodes.NotValid);
        RuleFor(x => x.Note).MaximumLength(2000).WithMessage(ErrorCodes.NotValid);
        RuleForEach(x => x.Exercises).SetValidator(new TrainingExerciseRequestValidator());
    }
}

/// <summary>
/// Every coach has their own templates, other coaches don't see them.
/// Administrators see and manage templates of all coaches.
/// </summary>
public static class TrainingTemplateAccess
{
    public static IQueryable<TrainingTemplate> GetVisibleTemplates(ApexPerformanceContext context,
        ICurrentUserService currentUserService)
        => TrainingAccess.IsAdministrator(currentUserService)
            ? context.TrainingTemplates
            : context.TrainingTemplates.Where(x => x.CreatedBy == currentUserService.UserId);

    public static bool CanEdit(TrainingTemplate template, ICurrentUserService currentUserService)
        => TrainingAccess.IsAdministrator(currentUserService) || template.CreatedBy == currentUserService.UserId;
}

public static class TrainingTemplateMapper
{
    public static TrainingTemplateResponse ToResponse(TrainingTemplate template, string? authorName, bool canEdit)
        => new(
            template.Id,
            template.Name,
            template.Note,
            template.Exercises
                .OrderBy(x => x.Order)
                .Select(x => new TrainingExerciseResponse(x.Id, x.WorkoutId, x.Workout.Name, x.Order, x.Note,
                    x.Sets
                        .OrderBy(set => set.Order)
                        .Select(set => new TrainingExerciseSetResponse(set.Id, set.Order, set.Reps, set.Weight))
                        .ToList(),
                    x.IsSupersetWithPrevious))
                .ToList(),
            authorName,
            canEdit,
            template.CreatedAt,
            template.UpdatedAt);

    public static List<TrainingTemplateExercise> ToExercises(IEnumerable<TrainingExerciseRequest>? exercises)
        => TrainingMapper.ToExercises(exercises).Select(x => new TrainingTemplateExercise
        {
            WorkoutId = x.WorkoutId,
            Order = x.Order,
            Note = x.Note,
            IsSupersetWithPrevious = x.IsSupersetWithPrevious,
            Sets = x.Sets.Select(set => new TrainingTemplateExerciseSet
            {
                Order = set.Order,
                Reps = set.Reps,
                Weight = set.Weight
            }).ToList()
        }).ToList();

    /// <summary>
    /// Exercises of a new training made from the template.
    /// </summary>
    public static List<TrainingExercise> ToTrainingExercises(TrainingTemplate template)
        => template.Exercises.OrderBy(x => x.Order).Select(x => new TrainingExercise
        {
            WorkoutId = x.WorkoutId,
            Order = x.Order,
            Note = x.Note,
            IsSupersetWithPrevious = x.IsSupersetWithPrevious,
            Sets = x.Sets.OrderBy(set => set.Order).Select(set => new TrainingExerciseSet
            {
                Order = set.Order,
                Reps = set.Reps,
                Weight = set.Weight
            }).ToList()
        }).ToList();

    /// <summary>
    /// Names of coaches and administrators who created the templates.
    /// </summary>
    public static Dictionary<Guid, string> GetAuthorNames(ApexPerformanceContext context, IEnumerable<Guid> userIds)
    {
        var ids = userIds.Distinct().ToList();

        return context.Users
            .Where(x => ids.Contains(x.Id))
            .Select(x => new
            {
                x.Id,
                Name = x.Coach != null
                    ? x.Coach.FirstName + " " + x.Coach.LastName
                    : x.Administrator != null
                        ? x.Administrator.FirstName + " " + x.Administrator.LastName
                        : x.UserName
            })
            .ToDictionary(x => x.Id, x => x.Name);
    }
}
