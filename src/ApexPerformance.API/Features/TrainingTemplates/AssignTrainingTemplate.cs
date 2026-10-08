using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Database.Entities;
using ApexPerformance.API.Features.Trainings;
using ApexPerformance.API.Services.Interfaces;
using FastEndpoints;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.TrainingTemplates;

public record AssignTrainingTemplateRequest(
    List<Guid> Clients,
    DateTimeOffset Date
);

public record AssignTrainingTemplateResponse(List<Guid> TrainingIds);

/// <summary>
/// Creates a planned training from the template for each client.
/// </summary>
public class AssignTrainingTemplateEndpoint
    : Endpoint<AssignTrainingTemplateRequest, AssignTrainingTemplateResponse>
{
    private readonly ApexPerformanceContext _context;
    private readonly ICurrentUserService _currentUserService;

    public AssignTrainingTemplateEndpoint(ApexPerformanceContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public override void Configure()
    {
        Post("api/training-templates/{id}/assign");
        Roles(UserRoles.SuperAdmin, UserRoles.Administrator, UserRoles.Coach);
        Options(x => x.WithTags("TrainingTemplates"));
    }

    public override async Task HandleAsync(AssignTrainingTemplateRequest request,
        CancellationToken cancellationToken)
    {
        var templateId = Route<Guid>("id", isRequired: true);

        var template = await TrainingTemplateAccess.GetVisibleTemplates(_context, _currentUserService)
            .AsNoTracking()
            .Include(x => x.Exercises)
            .ThenInclude(x => x.Sets)
            .FirstOrDefaultAsync(x => x.Id == templateId, cancellationToken);

        if (template is null)
            ThrowError(ErrorCodes.NotFound);

        var clientIds = request.Clients.Distinct().ToList();

        var clients = await _context.Clients
            .Where(x => clientIds.Contains(x.Id))
            .ToListAsync(cancellationToken);

        if (clients.Count != clientIds.Count)
            ThrowError(ErrorCodes.NotFound);

        foreach (var client in clients)
        {
            if (!await TrainingAccess.CanManageClient(_context, _currentUserService, client.Id, cancellationToken))
                ThrowError(ErrorCodes.NotFound);
        }

        var trainings = clients.Select(client => new Training
        {
            Name = template.Name,
            Date = request.Date,
            Note = template.Note,
            Client = client,
            Exercises = TrainingTemplateMapper.ToTrainingExercises(template)
        }).ToList();

        _context.Trainings.AddRange(trainings);

        var result = await _context.SaveChangesAsync(cancellationToken);

        if (result == 0)
            ThrowError(ErrorCodes.SavingError);

        await SendAsync(new AssignTrainingTemplateResponse(trainings.Select(x => x.Id).ToList()),
            cancellation: cancellationToken);
    }
}

public sealed class AssignTrainingTemplateValidator : Validator<AssignTrainingTemplateRequest>
{
    public AssignTrainingTemplateValidator()
    {
        RuleFor(x => x.Clients).NotEmpty().WithMessage(ErrorCodes.Required)
            .Must(x => x.Count <= 100).WithMessage(ErrorCodes.NotValid);
        RuleFor(x => x.Date).NotEmpty().WithMessage(ErrorCodes.Required);
    }
}
