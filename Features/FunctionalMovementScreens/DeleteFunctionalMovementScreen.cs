using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Features.BodyMeasurements;
using ApexPerformance.API.Services;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.FunctionalMovementScreens;

public class DeleteFunctionalMovementScreenEndpoint : EndpointWithoutRequest<int>
{
    private readonly ApexPerformanceContext _context;

    public DeleteFunctionalMovementScreenEndpoint(ApexPerformanceContext context)
    {
        _context = context;
    }

    public override void Configure()
    {
        Delete("api/functional-movement-screens/{id}");
        Permissions(UserPermissions.CanDeleteFunctionalMovementScreen);
        Options(x => x.WithTags("FunctionalMovementScreens"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var functionalMovementScreenId = Route<Guid>("id", isRequired: true);

        var functionalMovementScreen =
            await _context.FunctionalMovementScreens
                .FirstOrDefaultAsync(x => x.Id == functionalMovementScreenId, 
                    cancellationToken: cancellationToken);

        if (functionalMovementScreen is null)
            ThrowError(ErrorMessages.NotFound);

        functionalMovementScreen.Delete();

        _context.FunctionalMovementScreens.Update(functionalMovementScreen);

        var result = await _context.SaveChangesAsync(cancellationToken);

        if (result == 0)
            ThrowError(ErrorMessages.SavingError);

        await SendAsync(StatusCodes.Status200OK, cancellation: cancellationToken);
    }
}