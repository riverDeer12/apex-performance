using ApexPerformance.API.Database;
using ApexPerformance.API.Services;
using ApexPerformance.API.Shared.Localization;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.MeasurementUnits;

public record GetMeasurementUnitResponse(
    Guid Id,
    LocalizedProperty Name,
    string Symbol
);

public class GetMeasurementUnitsEndpoint : EndpointWithoutRequest<List<GetMeasurementUnitResponse>>
{
    private readonly ApexPerformanceContext _context;

    public GetMeasurementUnitsEndpoint(ApexPerformanceContext context)
    {
        _context = context;
    }

    public override void Configure()
    {
        Get("api/measurement-units");
        Options(x => x.WithTags("MeasurementUnits"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var measurementUnits = await
            _context.MeasurementUnits.ToListAsync(cancellationToken: cancellationToken);

        if (measurementUnits.Count is 0)
        {
            await SendAsync([], cancellation: cancellationToken);
            return;
        }

        await SendAsync(
            measurementUnits.Select(measurementUnit =>
                new GetMeasurementUnitResponse(measurementUnit.Id, new LocalizedProperty(measurementUnit.Name),
                    measurementUnit.Symbol)).ToList(),
            cancellation: cancellationToken);
    }
}