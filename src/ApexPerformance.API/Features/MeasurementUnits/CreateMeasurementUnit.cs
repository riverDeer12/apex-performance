using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Database.Entities;
using ApexPerformance.API.Utilities.Localization;
using FastEndpoints;

namespace ApexPerformance.API.Features.MeasurementUnits;

public record CreateMeasurementUnitRequest(
    LocalizedProperty Name,
    string Symbol
);

public class CreateMeasurementUnitEndpoint : Endpoint<CreateMeasurementUnitRequest, GetMeasurementUnitResponse>
{
    private readonly ApexPerformanceContext _context;

    public CreateMeasurementUnitEndpoint(ApexPerformanceContext context)
    {
        _context = context;
    }

    public override void Configure()
    {
        Post("api/measurement-units");
        Options(x => x.WithTags("MeasurementUnits"));
    }

    public override async Task HandleAsync(CreateMeasurementUnitRequest request, CancellationToken cancellationToken)
    {
        var measurementUnit = new MeasurementUnit
        {
            Name = request.Name.ToJsonString(),
            Symbol = request.Symbol
        };

        _context.MeasurementUnits.Add(measurementUnit);

        var result = await _context.SaveChangesAsync(cancellationToken);

        if (result == 0)
            ThrowError(ErrorCodes.SavingError);

        await SendAsync(
            new GetMeasurementUnitResponse(measurementUnit.Id, new LocalizedProperty(measurementUnit.Name), measurementUnit.Symbol),
            cancellation: cancellationToken);
    }
}