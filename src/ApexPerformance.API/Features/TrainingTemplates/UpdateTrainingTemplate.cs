using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Features.Trainings;
using ApexPerformance.API.Services.Interfaces;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.TrainingTemplates;

public class UpdateTrainingTemplateEndpoint : Endpoint<TrainingTemplateRequest, TrainingTemplateResponse>
{
    private readonly ApexPerformanceContext _context;
    private readonly ICurrentUserService _currentUserService;

    public UpdateTrainingTemplateEndpoint(ApexPerformanceContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public override void Configure()
    {
        Put("api/training-templates/{id}");
        Roles(UserRoles.SuperAdmin, UserRoles.Administrator, UserRoles.Coach);
        Options(x => x.WithTags("TrainingTemplates"));
    }

    public override async Task HandleAsync(TrainingTemplateRequest request, CancellationToken cancellationToken)
    {
        var templateId = Route<Guid>("id", isRequired: true);

        var template = await TrainingTemplateAccess.GetVisibleTemplates(_context, _currentUserService)
            .Include(x => x.Exercises)
            .ThenInclude(x => x.Sets)
            .FirstOrDefaultAsync(x => x.Id == templateId, cancellationToken);

        if (template is null)
            ThrowError(ErrorCodes.NotFound);

        if (!TrainingTemplateAccess.CanEdit(template, _currentUserService))
            ThrowError(ErrorCodes.UnauthorizedAction, StatusCodes.Status403Forbidden);

        if (!await TrainingValidation.WorkoutsExist(_context, request.Exercises, cancellationToken))
            ThrowError(ErrorCodes.NotValid);

        template.Name = request.Name.Trim();
        template.Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim();

        // Exercises are replaced, their order comes from the request.
        _context.TrainingTemplateExercises.RemoveRange(template.Exercises);
        template.Exercises = TrainingTemplateMapper.ToExercises(request.Exercises);

        var result = await _context.SaveChangesAsync(cancellationToken);

        if (result == 0)
            ThrowError(ErrorCodes.SavingError);

        await _context.Entry(template).Collection(x => x.Exercises).Query()
            .Include(x => x.Workout)
            .Include(x => x.Sets)
            .LoadAsync(cancellationToken);

        var authorNames = TrainingTemplateMapper.GetAuthorNames(_context, [template.CreatedBy]);

        await SendAsync(TrainingTemplateMapper.ToResponse(template, authorNames.GetValueOrDefault(template.CreatedBy),
            true), cancellation: cancellationToken);
    }
}
