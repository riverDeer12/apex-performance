using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Database.Entities;
using ApexPerformance.API.Features.Trainings;
using ApexPerformance.API.Services.Interfaces;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.TrainingTemplates;

public class CreateTrainingTemplateEndpoint : Endpoint<TrainingTemplateRequest, TrainingTemplateResponse>
{
    private readonly ApexPerformanceContext _context;
    private readonly ICurrentUserService _currentUserService;

    public CreateTrainingTemplateEndpoint(ApexPerformanceContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public override void Configure()
    {
        Post("api/training-templates");
        Roles(UserRoles.SuperAdmin, UserRoles.Administrator, UserRoles.Coach);
        Options(x => x.WithTags("TrainingTemplates"));
    }

    public override async Task HandleAsync(TrainingTemplateRequest request, CancellationToken cancellationToken)
    {
        if (!await TrainingValidation.WorkoutsExist(_context, request.Exercises, cancellationToken))
            ThrowError(ErrorCodes.NotValid);

        var template = new TrainingTemplate
        {
            Name = request.Name.Trim(),
            Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim(),
            Exercises = TrainingTemplateMapper.ToExercises(request.Exercises)
        };

        _context.TrainingTemplates.Add(template);

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
