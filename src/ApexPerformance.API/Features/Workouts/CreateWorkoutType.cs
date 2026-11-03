using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Database.Entities.Catalog;
using ApexPerformance.API.Shared.DataTransferObjects;
using ApexPerformance.API.Utilities.Localization;
using FastEndpoints;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Workouts;

public record CreateWorkoutTypeRequest(string NameHr, string? NameEn);

/// <summary>
/// Adds a workout type with Croatian and optional English name. Types are
/// shared by all coaches, so only administrators can add them.
/// </summary>
public class CreateWorkoutTypeEndpoint : Endpoint<CreateWorkoutTypeRequest, StatusResponse>
{
    private const int MaxNameLength = 500;

    private readonly ApexPerformanceContext _context;

    public CreateWorkoutTypeEndpoint(ApexPerformanceContext context)
    {
        _context = context;
    }

    public override void Configure()
    {
        Post("api/workout-types");
        Roles(UserRoles.SuperAdmin, UserRoles.Administrator);
        Options(x => x.WithTags("Workouts"));
    }

    public override async Task HandleAsync(CreateWorkoutTypeRequest request, CancellationToken cancellationToken)
    {
        var nameHr = request.NameHr.Trim();
        var nameEn = request.NameEn?.Trim();

        // Two types with the same Croatian name can't be told apart.
        var existing = await _context.WorkoutTypes
            .AsNoTracking()
            .Select(x => x.Name)
            .ToListAsync(cancellationToken);

        if (existing.Any(x => string.Equals(new LocalizedProperty(x).Get(Language.HR)?.Trim(), nameHr,
                StringComparison.OrdinalIgnoreCase)))
            ThrowError(ErrorCodes.AlreadyExists);

        var name = new LocalizedProperty();
        name.Translations[Language.HR] = nameHr;

        if (!string.IsNullOrWhiteSpace(nameEn))
            name.Translations[Language.EN] = nameEn;

        var json = name.ToJsonString();

        if (json.Length > MaxNameLength)
            ThrowError(ErrorCodes.NotValid);

        // The name is the description too, like the imported types.
        var workoutType = new WorkoutType { Name = json, Description = json };

        _context.WorkoutTypes.Add(workoutType);

        await _context.SaveChangesAsync(cancellationToken);

        await SendAsync(new StatusResponse(workoutType.Id, true), cancellation: cancellationToken);
    }
}

public sealed class CreateWorkoutTypeValidator : Validator<CreateWorkoutTypeRequest>
{
    public CreateWorkoutTypeValidator()
    {
        RuleFor(x => x.NameHr).NotEmpty().WithMessage(ErrorCodes.Required)
            .MaximumLength(200).WithMessage(ErrorCodes.NotValid);
        RuleFor(x => x.NameEn).MaximumLength(200).WithMessage(ErrorCodes.NotValid);
    }
}
