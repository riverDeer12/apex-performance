using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Services;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.BodyMeasurements;

public record GetCoachClientsBodyMeasurementsResponse(
    Guid Id,
    string FirstName,
    string LastName,
    DateTimeOffset CreatedAt,
    decimal Weight,
    decimal Shoulders,
    decimal Chest,
    decimal UpperArm,
    decimal Waist,
    decimal Thigh,
    decimal Calves);

public class
    GetCoachClientsBodyMeasurementsEndpoint : EndpointWithoutRequest<List<GetCoachClientsBodyMeasurementsResponse>>
{
    private readonly ApexPerformanceContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetCoachClientsBodyMeasurementsEndpoint(ApexPerformanceContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public override void Configure()
    {
        Get("api/body-measurements/coach");
        Options(x => x.WithTags("BodyMeasurements"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var coach = await _context.Coaches.FirstOrDefaultAsync(x => x.UserId == _currentUserService.UserId,
            cancellationToken: cancellationToken);

        if (coach is null)
            ThrowError(ErrorMessages.NotFound);

        var clientIds = await _context.CoachClients.Where(x => x.CoachId == coach.Id)
            .Select(x => x.ClientId)
            .ToListAsync(cancellationToken: cancellationToken);

        if (clientIds.Count == 0)
        {
            await SendAsync([], cancellation: cancellationToken);
            return;
        }

        var bodyMeasurements = await _context.BodyMeasurements
            .Where(x => clientIds.Contains(x.ClientId) && !x.IsDeleted)
            .Include(bodyMeasurement => bodyMeasurement.Client)
            .ToListAsync(cancellationToken: cancellationToken);

        await SendAsync(bodyMeasurements.Select(x =>
                new GetCoachClientsBodyMeasurementsResponse(x.Id, x.Client.FirstName, x.Client.LastName, x.CreatedAt,
                    x.Weight, x.Shoulders, x.Chest, x.UpperArm, x.Waist, x.Thigh, x.Calves))
            .ToList(), cancellation: cancellationToken);
    }
}