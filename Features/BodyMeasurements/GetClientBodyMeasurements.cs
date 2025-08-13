using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Services;
using ApexPerformance.API.Shared.DataTransferObjects;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.BodyMeasurements;

public record GetClientBodyMeasurementsResponse(
    Guid Id,
    decimal Height,
    decimal Weight,
    decimal Shoulders,
    decimal Chest,
    decimal UpperArm,
    decimal Waist,
    decimal Thigh,
    decimal Calves,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    PersonDataDto Client
);

public class GetClientBodyMeasurementsEndpoint : EndpointWithoutRequest<List<GetClientBodyMeasurementsResponse>>
{
    private readonly ApexPerformanceContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetClientBodyMeasurementsEndpoint(ICurrentUserService currentUserService, 
        ApexPerformanceContext context)
    {
        _currentUserService = currentUserService;
        _context = context;
    }

    public override void Configure()
    {
        Get("api/body-measurements/client");
        Options(x => x.WithTags("BodyMeasurements"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var client = await _context.Clients.FirstOrDefaultAsync(x => x.UserId == _currentUserService.UserId,
            cancellationToken: cancellationToken);

        if (client is null)
            ThrowError(ErrorMessages.NotFound);

        var bodyMeasurements = await _context.BodyMeasurements
            .Where(x => x.ClientId == client.Id && !x.IsDeleted)
            .Include(bodyMeasurement => bodyMeasurement.Client)
            .ToListAsync(cancellationToken: cancellationToken);

        if (bodyMeasurements.Count is 0)
        {
            await SendAsync([], cancellation: cancellationToken);
            return;
        }

        await SendAsync(bodyMeasurements
            .Select(x =>
                new GetClientBodyMeasurementsResponse(x.Id, x.Height, x.Weight, x.Shoulders, x.Chest, x.UpperArm,
                    x.Waist,
                    x.Thigh, x.Calves, x.CreatedAt, x.UpdatedAt,
                    new PersonDataDto(x.Client.Id, x.Client.FirstName, x.Client.LastName)))
            .ToList(), cancellation: cancellationToken);
    }
}