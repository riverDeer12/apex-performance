using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Database.Entities;
using ApexPerformance.API.Shared.Localization;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.MeasurementUnits;

public record UpdateMeasurementUnitRequest(
    LocalizedProperty Name,
    string Symbol
);

public class UpdateMeasurementUnitEndpoint : Endpoint<UpdateMeasurementUnitRequest, GetMeasurementUnitResponse>
{
    private readonly ApexPerformanceContext _context;

    public UpdateMeasurementUnitEndpoint(ApexPerformanceContext context)
    {
        _context = context;
    }

    public override void Configure()
    {
        Put("api/measurement-units/{id}");
        Options(x => x.WithTags("MeasurementUnits"));
    }

    public override async Task HandleAsync(UpdateMeasurementUnitRequest request, CancellationToken cancellationToken)
    {
        var measurementUnitId = Route<Guid>("id", isRequired: true);

        var measurementUnit =
            await _context.MeasurementUnits
                .FirstOrDefaultAsync(x => x.Id == measurementUnitId, cancellationToken: cancellationToken);

        if (measurementUnit is null)
            ThrowError(ErrorCodes.NotFound);
        
        measurementUnit.Name = request.Name.ToJsonString();
        measurementUnit.Symbol = request.Symbol;
        
        _context.MeasurementUnits.Update(measurementUnit);

        var result = await _context.SaveChangesAsync(cancellationToken);

        if (result == 0)
            ThrowError(ErrorCodes.SavingError);

        await SendAsync(
            new GetMeasurementUnitResponse(measurementUnit.Id, new LocalizedProperty(measurementUnit.Name), measurementUnit.Symbol),
            cancellation: cancellationToken);
    }
}