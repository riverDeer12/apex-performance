using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Shared.DataTransferObjects;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.MeasurementUnits;

public class DeleteMeasurementUnitEndpoint : EndpointWithoutRequest<StatusResponse>
{
    private readonly ApexPerformanceContext _context;

    public DeleteMeasurementUnitEndpoint(ApexPerformanceContext context)
    {
        _context = context;
    }

    public override void Configure()
    {
        Delete("api/measurement-units/{id}");
        Roles(UserRoles.SuperAdmin);
        Options(x => x.WithTags("MeasurementUnits"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var measurementUnitId = Route<Guid>("id", isRequired: true);

        var measurementUnit =
            await _context.MeasurementUnits
                .FirstOrDefaultAsync(x => x.Id == measurementUnitId, 
                    cancellationToken: cancellationToken);

        if (measurementUnit is null)
            ThrowError(ErrorCodes.NotFound);

        measurementUnit.Delete();

        _context.MeasurementUnits.Update(measurementUnit);

        var result = await _context.SaveChangesAsync(cancellationToken);

        if (result == 0)
            ThrowError(ErrorCodes.SavingError);

        await SendAsync(new StatusResponse(measurementUnit.Id, true), cancellation: cancellationToken);
    }
}