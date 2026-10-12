using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Shared.DataTransferObjects;
using ApexPerformance.API.Utilities.Localization;
using FastEndpoints;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Workouts;

public record UpdateWorkoutTypeRequest(string NameHr, string? NameEn);

/// <summary>
/// Renames a workout type. Croatian and English names are set,
/// other existing translations are kept. Types are shared by all
/// coaches, so only administrators can change them.
/// </summary>
public class UpdateWorkoutTypeEndpoint : Endpoint<UpdateWorkoutTypeRequest, StatusResponse>
{
    private const int MaxNameLength = 500;

    private readonly ApexPerformanceContext _context;

    public UpdateWorkoutTypeEndpoint(ApexPerformanceContext context)
    {
        _context = context;
    }

    public override void Configure()
    {
        Put("api/workout-types/{id}");
        Roles(UserRoles.SuperAdmin, UserRoles.Administrator);
        Options(x => x.WithTags("Workouts"));
    }

    public override async Task HandleAsync(UpdateWorkoutTypeRequest request, CancellationToken cancellationToken)
    {
        var workoutTypeId = Route<Guid>("id", isRequired: true);

        var workoutType = await _context.WorkoutTypes
            .FirstOrDefaultAsync(x => x.Id == workoutTypeId, cancellationToken);

        if (workoutType is null)
            ThrowError(ErrorCodes.NotFound, StatusCodes.Status404NotFound);

        var nameHr = request.NameHr.Trim();
        var nameEn = request.NameEn?.Trim();

        // Two types with the same Croatian name can't be told apart.
        var others = await _context.WorkoutTypes
            .AsNoTracking()
            .Where(x => x.Id != workoutTypeId)
            .Select(x => x.Name)
            .ToListAsync(cancellationToken);

        if (others.Any(x => string.Equals(new LocalizedProperty(x).Get(Language.HR)?.Trim(), nameHr,
                StringComparison.OrdinalIgnoreCase)))
            ThrowError(ErrorCodes.AlreadyExists);

        var previousName = workoutType.Name;

        var name = new LocalizedProperty(workoutType.Name);
        name.Translations[Language.HR] = nameHr;

        if (string.IsNullOrWhiteSpace(nameEn))
            name.Translations.Remove(Language.EN);
        else
            name.Translations[Language.EN] = nameEn;

        var json = name.ToJsonString();

        if (json.Length > MaxNameLength)
            ThrowError(ErrorCodes.NotValid);

        workoutType.Name = json;

        // Imported types have the name as description too, it is kept the same.
        if (workoutType.Description == previousName)
            workoutType.Description = json;

        await _context.SaveChangesAsync(cancellationToken);

        await SendAsync(new StatusResponse(workoutType.Id, true), cancellation: cancellationToken);
    }
}

public sealed class UpdateWorkoutTypeValidator : Validator<UpdateWorkoutTypeRequest>
{
    public UpdateWorkoutTypeValidator()
    {
        RuleFor(x => x.NameHr).NotEmpty().WithMessage(ErrorCodes.Required)
            .MaximumLength(200).WithMessage(ErrorCodes.NotValid);
        RuleFor(x => x.NameEn).MaximumLength(200).WithMessage(ErrorCodes.NotValid);
    }
}
