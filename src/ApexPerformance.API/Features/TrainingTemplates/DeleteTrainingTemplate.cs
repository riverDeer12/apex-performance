using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Services.Interfaces;
using ApexPerformance.API.Shared.DataTransferObjects;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.TrainingTemplates;

public class DeleteTrainingTemplateEndpoint : EndpointWithoutRequest<StatusResponse>
{
    private readonly ApexPerformanceContext _context;
    private readonly ICurrentUserService _currentUserService;

    public DeleteTrainingTemplateEndpoint(ApexPerformanceContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public override void Configure()
    {
        Delete("api/training-templates/{id}");
        Roles(UserRoles.SuperAdmin, UserRoles.Administrator, UserRoles.Coach);
        Options(x => x.WithTags("TrainingTemplates"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var templateId = Route<Guid>("id", isRequired: true);

        var template = await TrainingTemplateAccess.GetVisibleTemplates(_context, _currentUserService)
            .FirstOrDefaultAsync(x => x.Id == templateId, cancellationToken);

        if (template is null)
            ThrowError(ErrorCodes.NotFound);

        if (!TrainingTemplateAccess.CanEdit(template, _currentUserService))
            ThrowError(ErrorCodes.UnauthorizedAction, StatusCodes.Status403Forbidden);

        template.Delete();

        var result = await _context.SaveChangesAsync(cancellationToken);

        if (result == 0)
            ThrowError(ErrorCodes.SavingError);

        await SendAsync(new StatusResponse(template.Id, true), cancellation: cancellationToken);
    }
}
