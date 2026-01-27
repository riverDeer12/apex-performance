using ApexPerformance.API.Database;
using ApexPerformance.API.Features.Ingredients;
using ApexPerformance.API.Services;
using ApexPerformance.API.Shared.Localization;
using FastEndpoints;

namespace ApexPerformance.API.Features.MeasurementUnits;

public record GetMeasurementUnitResponse(
    LocalizedProperty Name,
    string Symbol
);

public class GetMeasurementUnitsEndpoint : EndpointWithoutRequest<List<GetMeasurementUnitResponse>>
{
    private readonly ApexPerformanceContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetMeasurementUnitsEndpoint(ApexPerformanceContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public override void Configure()
    {
        Get("api/measurement-units");
        Options(x => x.WithTags("MeasurementUnits"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        
    }
}