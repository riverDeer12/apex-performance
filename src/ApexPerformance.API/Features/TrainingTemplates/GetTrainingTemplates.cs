using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Services.Interfaces;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.TrainingTemplates;

public class GetTrainingTemplatesEndpoint : EndpointWithoutRequest<List<TrainingTemplateResponse>>
{
    private readonly ApexPerformanceContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetTrainingTemplatesEndpoint(ApexPerformanceContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public override void Configure()
    {
        Get("api/training-templates");
        Roles(UserRoles.SuperAdmin, UserRoles.Administrator, UserRoles.Coach);
        Options(x => x.WithTags("TrainingTemplates"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var templates = await _context.TrainingTemplates
            .AsNoTracking()
            .Include(x => x.Exercises)
            .ThenInclude(x => x.Workout)
            .Include(x => x.Exercises)
            .ThenInclude(x => x.Sets)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

        var authorNames = TrainingTemplateMapper.GetAuthorNames(_context, templates.Select(x => x.CreatedBy));

        await SendAsync(templates
                .Select(x => TrainingTemplateMapper.ToResponse(x, authorNames.GetValueOrDefault(x.CreatedBy),
                    TrainingTemplateAccess.CanEdit(x, _currentUserService)))
                .ToList(),
            cancellation: cancellationToken);
    }
}
